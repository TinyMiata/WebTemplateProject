using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace WebTemplate.EditorTools
{
    /// <summary>
    /// Single build-size dashboard for the web template. Replaces the old
    /// "WebGL Build Size Inspector" and "WebGL Deep Build Inspector" windows.
    ///
    /// Shows, in one place:
    ///   - a checklist of the size-critical Player Settings,
    ///   - the largest source assets by type,
    ///   - installed packages ranked by on-disk size,
    ///   - the file breakdown of the last WebGL build under Builds/Web/Build.
    /// </summary>
    public class WebSizeReport : EditorWindow
    {
        const string BuildDir = "Builds/Web/Build";
        const string BundleDir = "Builds/Web/StreamingAssets/aa/WebGL";

        Vector2 scroll;

        class Entry
        {
            public string label;
            public long size;
            public Object asset;
        }

        readonly List<Entry> textures = new();
        readonly List<Entry> audio = new();
        readonly List<Entry> meshes = new();
        readonly List<Entry> packages = new();
        readonly List<Entry> buildFiles = new();
        readonly List<Entry> bundles = new();

        long assetTotal;
        long buildTotal;
        long bundleTotal;
        ListRequest packageRequest;

        [MenuItem("Tools/Web Build/Size Report")]
        public static void Open() => GetWindow<WebSizeReport>("Web Size Report");

        void OnEnable() => ScanBuildOutput();

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Project + Packages")) ScanProject();
                if (GUILayout.Button("Refresh Build Output")) ScanBuildOutput();
                if (GUILayout.Button("Apply Lean Settings")) WebBuild.ApplyLeanSettings();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSettingsChecklist();
            DrawBuildOutput();
            DrawBundles();
            DrawList("Largest Textures", textures);
            DrawList("Largest Audio Clips", audio);
            DrawList("Largest Meshes", meshes);
            DrawList("Installed Packages (on-disk)", packages);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField($"Approx raw source-asset size: {WebBuild.Fmt(assetTotal)}");

            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------- scans

        void ScanProject()
        {
            textures.Clear();
            audio.Clear();
            meshes.Clear();
            assetTotal = 0;

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets"))
                    continue;

                long size;
                try { size = new FileInfo(path).Length; }
                catch { continue; }

                assetTotal += size;

                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj is Texture)
                    textures.Add(new Entry { label = path, size = size, asset = obj });
                else if (obj is AudioClip)
                    audio.Add(new Entry { label = path, size = size, asset = obj });
                else if (obj is Mesh)
                    meshes.Add(new Entry { label = path, size = size, asset = obj });
            }

            Trim(textures);
            Trim(audio);
            Trim(meshes);

            packages.Clear();
            packageRequest = Client.List(true);
            EditorApplication.update += PollPackages;
        }

        void PollPackages()
        {
            if (packageRequest == null || !packageRequest.IsCompleted)
                return;

            EditorApplication.update -= PollPackages;

            if (packageRequest.Status == StatusCode.Success)
            {
                foreach (var p in packageRequest.Result)
                    packages.Add(new Entry { label = $"{p.name}  {p.version}", size = DirSize(p.resolvedPath) });

                packages.Sort((a, b) => b.size.CompareTo(a.size));
            }

            Repaint();
        }

        void ScanBuildOutput()
        {
            buildFiles.Clear();
            buildTotal = 0;

            if (!Directory.Exists(BuildDir))
                return;

            foreach (var f in new DirectoryInfo(BuildDir).GetFiles())
            {
                buildTotal += f.Length;
                buildFiles.Add(new Entry { label = f.Name, size = f.Length });
            }

            buildFiles.Sort((a, b) => b.size.CompareTo(a.size));

            bundles.Clear();
            bundleTotal = 0;

            if (Directory.Exists(BundleDir))
            {
                foreach (var f in new DirectoryInfo(BundleDir).GetFiles("*", SearchOption.AllDirectories))
                {
                    bundleTotal += f.Length;
                    bundles.Add(new Entry { label = f.Name, size = f.Length });
                }
                bundles.Sort((a, b) => b.size.CompareTo(a.size));
            }
        }

        // ---------------------------------------------------------------- draw

        void DrawSettingsChecklist()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Build Settings Checklist", EditorStyles.boldLabel);

            Check("Compression = Brotli",
                PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli);
            Check("Decompression Fallback = Off",
                !PlayerSettings.WebGL.decompressionFallback);
            Check("Exceptions = None",
                PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.None);
            Check("Strip Engine Code",
                PlayerSettings.stripEngineCode);
            Check("Managed Stripping = High",
                PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL) == ManagedStrippingLevel.High);
            Check("IL2CPP Code Generation = Size",
                PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL) == Il2CppCodeGeneration.OptimizeSize);
            Check("Debug Symbols = Off",
                PlayerSettings.WebGL.debugSymbolMode == WebGLDebugSymbolMode.Off);
            Check("Data Caching",
                PlayerSettings.WebGL.dataCaching);
            Check("Unity Splash Disabled",
                !PlayerSettings.SplashScreen.show);
            Check("Development Build Disabled",
                !EditorUserBuildSettings.development);
            Check("WebGL template = poki-template",
                PlayerSettings.WebGL.template == "PROJECT:poki-template");
        }

        void DrawBuildOutput()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                Directory.Exists(BuildDir)
                    ? $"Last Build Output  —  TOTAL {WebBuild.Fmt(buildTotal)}"
                    : "Last Build Output  —  (no build found at Builds/Web/Build)",
                EditorStyles.boldLabel);

            foreach (var e in buildFiles)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(WebBuild.Fmt(e.size), GUILayout.Width(90));
                    EditorGUILayout.LabelField(e.label);
                }
        }

        void DrawBundles()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                Directory.Exists(BundleDir)
                    ? $"Addressables Bundles (fetched post-boot, not initial payload)  —  TOTAL {WebBuild.Fmt(bundleTotal)}"
                    : "Addressables Bundles  —  (none at Builds/Web/StreamingAssets/aa/WebGL)",
                EditorStyles.boldLabel);

            foreach (var e in bundles)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(WebBuild.Fmt(e.size), GUILayout.Width(90));
                    EditorGUILayout.LabelField(e.label);
                }
        }

        void DrawList(string title, List<Entry> list)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("  (run a scan)");
                return;
            }

            foreach (var e in list)
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (e.asset != null && GUILayout.Button("Select", GUILayout.Width(55)))
                    {
                        Selection.activeObject = e.asset;
                        EditorGUIUtility.PingObject(e.asset);
                    }
                    else if (e.asset == null)
                    {
                        GUILayout.Space(59);
                    }

                    EditorGUILayout.LabelField(WebBuild.Fmt(e.size), GUILayout.Width(90));
                    EditorGUILayout.LabelField(e.label);
                }
        }

        void Check(string label, bool ok)
        {
            var prev = GUI.color;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = ok ? Color.green : new Color(1f, 0.75f, 0.2f);
                EditorGUILayout.LabelField(ok ? "OK" : "!!", GUILayout.Width(24));
                GUI.color = prev;
                EditorGUILayout.LabelField(label);
            }
        }

        // -------------------------------------------------------------- helpers

        static void Trim(List<Entry> list)
        {
            list.Sort((a, b) => b.size.CompareTo(a.size));
            if (list.Count > 20)
                list.RemoveRange(20, list.Count - 20);
        }

        static long DirSize(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return 0;

            long total = 0;
            foreach (var f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(f).Length; }
                catch { /* ignore unreadable */ }
            }
            return total;
        }
    }
}
