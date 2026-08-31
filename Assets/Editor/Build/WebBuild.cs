using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WebTemplate.EditorTools
{
    /// <summary>
    /// One-click WebGL build for the lightweight web template.
    ///
    /// Re-asserts every size / startup-critical Player Setting at build time, so a
    /// build is always lean no matter how the project settings drifted in the
    /// Editor, then prints the compressed payload size with a budget warning.
    /// </summary>
    public static class WebBuild
    {
        const string TemplateName = "PROJECT:poki-template";
        const string TemplateFolder = "Assets/WebGLTemplates/poki-template";
        const string OutputPath = "Builds/Web";

        // Single archive to hand to Poki: the whole build folder (Build/ +
        // StreamingAssets/ + index.html/json + screenshots/ + thumbnail) with
        // index.html at the zip root. Sibling of OutputPath so it is never
        // nested into itself.
        const string PokiZipPath = "Builds/Web-Poki.zip";

        // Warn if the compressed payload (everything under Build/) exceeds this.
        const long PayloadBudgetBytes = 15L * 1024 * 1024;

        [MenuItem("Tools/Web Build/Build WebGL %#b")]
        public static void BuildMenu()
        {
            var report = Run();
            if (report != null && report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.GetFullPath(OutputPath));
        }

        /// <summary>
        /// CI entry point:
        /// Unity -batchmode -quit -projectPath . -executeMethod WebTemplate.EditorTools.WebBuild.BuildCLI
        /// </summary>
        public static void BuildCLI()
        {
            var report = Run();
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static BuildReport Run()
        {
            if (!Directory.Exists(TemplateFolder))
            {
                Debug.LogError($"[WebBuild] Required WebGL template missing: {TemplateFolder}. Aborting.");
                return null;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            ApplyLeanSettings();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[WebBuild] No enabled scenes in Build Settings. Aborting.");
                return null;
            }

            // Addressables content is (re)built and copied into StreamingAssets by
            // the player build itself — see BuildAddressablesWithPlayerBuild in
            // ApplyLeanSettings(). The menu item below is for content-only iteration.

            Directory.CreateDirectory(OutputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            ReportPayload(report);

            if (report != null && report.summary.result == BuildResult.Succeeded)
                PackageForPoki();

            return report;
        }

        /// <summary>
        /// Zips the entire build output (Build/ + StreamingAssets/ + template
        /// files) into a single archive ready to upload to Poki, with index.html
        /// at the archive root.
        /// </summary>
        [MenuItem("Tools/Web Build/Package for Poki (zip)")]
        public static void PackageForPoki()
        {
            if (!Directory.Exists(OutputPath))
            {
                Debug.LogError($"[WebBuild] Nothing to package — {OutputPath} does not exist. Build first.");
                return;
            }

            // Drop any archive sitting inside the build folder (stale hand-made
            // Build.zip, "Build (2).zip" copies, a previous run's output, ...) so
            // nothing stray gets swept into the new package.
            foreach (var z in Directory.GetFiles(OutputPath, "*.zip", SearchOption.AllDirectories))
                File.Delete(z);

            if (File.Exists(PokiZipPath))
                File.Delete(PokiZipPath);

            // Fully-qualified: UnityEngine also defines a CompressionLevel.
            ZipFile.CreateFromDirectory(OutputPath, PokiZipPath,
                System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            long zipSize = new FileInfo(PokiZipPath).Length;
            Debug.Log(
                $"[WebBuild] Poki upload package: {Path.GetFullPath(PokiZipPath)} ({Fmt(zipSize)}). " +
                "Includes Build/ + StreamingAssets/ (Addressables) + index.html/json + screenshots/.");
        }

        /// <summary>
        /// Rebuilds the Addressables content (catalog + LZ4 bundles into
        /// StreamingAssets/aa/WebGL). Run automatically before every player build;
        /// also exposed as a menu item for content-only iteration.
        /// </summary>
        [MenuItem("Tools/Web Build/Build Addressables")]
        public static bool BuildAddressables()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                Debug.LogError("[WebBuild] No Addressables settings. Open " +
                    "Window > Asset Management > Addressables > Groups to create them.");
                return false;
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"[WebBuild] Addressables content build failed: {result.Error}");
                return false;
            }

            Debug.Log($"[WebBuild] Addressables content built in {result.Duration:0.0}s -> {result.OutputPath}");
            return true;
        }

        /// <summary>
        /// The single source of truth for "what makes this build lightweight".
        /// Also callable on its own via the menu to fix a drifted project.
        /// </summary>
        [MenuItem("Tools/Web Build/Apply Lean Settings")]
        public static void ApplyLeanSettings()
        {
            var webgl = NamedBuildTarget.WebGL;

            // --- Delivery -------------------------------------------------------
            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false; // Poki serves correct headers
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            // --- Code size ----------------------------------------------------
            PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCodeGeneration(webgl, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(webgl, Il2CppCompilerConfiguration.Master);

            // Smallest emitted wasm: LLVM optimises for size + link-time optimisation.
            // Costs build time only; nothing is removed from the runtime.
            // The 6000.3 API lives in UnityEditor.WebGL, not PlayerSettings.WebGL.
            UnityEditor.WebGL.UserBuildSettings.codeOptimization =
                UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;

            // NOTE: do NOT add "--closure 1" here. Unity 6000.3.10f1's bundled
            // emscripten emits framework JS that its own Closure compiler rejects
            // ("Variable abort declared more than once"), which hard-fails the
            // build. Keep emscriptenArgs clear unless a specific arg is needed.
            PlayerSettings.WebGL.emscriptenArgs = string.Empty;
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.High);
            PlayerSettings.SetApiCompatibilityLevel(webgl, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true; // drop unused vertex channels
            PlayerSettings.bakeCollisionMeshes = true;       // pre-cook at build time, not on load
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.threadsSupport = false;

            // --- Runtime footprint ------------------------------------------
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = 32;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.runInBackground = false;

            // --- No dev / debug bloat -------------------------------------
            EditorUserBuildSettings.development = false;
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);

            // Guarantee the Addressables catalog + bundles are rebuilt and copied
            // into the player's StreamingAssets on every WebGL build.
            var aa = AddressableAssetSettingsDefaultObject.Settings;
            if (aa != null)
                aa.BuildAddressablesWithPlayerBuild =
                    AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

            AssetDatabase.SaveAssets();
            Debug.Log("[WebBuild] Lean settings applied.");
        }

        static void ReportPayload(BuildReport report)
        {
            if (report == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(
                $"[WebBuild] Result: {report.summary.result}  |  " +
                $"Unity reported total: {Fmt((long)report.summary.totalSize)}  |  " +
                $"Build time: {report.summary.totalTime:mm\\:ss}");

            string buildDir = Path.Combine(OutputPath, "Build");
            if (Directory.Exists(buildDir))
            {
                long total = 0;
                var files = new DirectoryInfo(buildDir).GetFiles().OrderByDescending(f => f.Length);

                sb.AppendLine($"--- {buildDir} (shipped payload) ---");
                foreach (var f in files)
                {
                    total += f.Length;
                    sb.AppendLine($"  {Fmt(f.Length),12}   {f.Name}");
                }
                sb.AppendLine($"  {Fmt(total),12}   TOTAL");

                if (total > PayloadBudgetBytes)
                    Debug.LogWarning(
                        $"[WebBuild] Payload {Fmt(total)} exceeds budget {Fmt(PayloadBudgetBytes)} — " +
                        "run Tools/Web Build/Size Report to find the offenders.");
            }

            // Addressables bundles ship alongside the player but are fetched AFTER
            // boot, so they are reported separately and not counted against the
            // initial-payload budget.
            string aaDir = Path.Combine(OutputPath, "StreamingAssets", "aa", "WebGL");
            if (Directory.Exists(aaDir))
            {
                long aaTotal = 0;
                var aaFiles = new DirectoryInfo(aaDir)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .OrderByDescending(f => f.Length);

                sb.AppendLine($"--- {aaDir} (Addressables, fetched post-boot) ---");
                foreach (var f in aaFiles)
                {
                    aaTotal += f.Length;
                    sb.AppendLine($"  {Fmt(f.Length),12}   {f.Name}");
                }
                sb.AppendLine($"  {Fmt(aaTotal),12}   ADDRESSABLES TOTAL");
            }

            Debug.Log(sb.ToString());
        }

        internal static string Fmt(long bytes)
        {
            if (bytes >= 1024 * 1024) return (bytes / (1024f * 1024f)).ToString("0.00") + " MB";
            if (bytes >= 1024) return (bytes / 1024f).ToString("0.00") + " KB";
            return bytes + " B";
        }
    }
}
