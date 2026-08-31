using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WebTemplate.EditorTools
{
    /// <summary>
    /// Lightweight-WebGL defaults for texture imports.
    ///
    /// Applied only the FIRST time an asset is imported (importSettingsMissing),
    /// so any manual per-texture change an artist makes afterwards is kept.
    /// Use "Tools/Web Build/Reimport Textures With Defaults" to force every
    /// existing texture back to these values.
    ///
    /// Folder heuristic: a path containing /ui/, /sprites/ or /icons/ is treated
    /// as UI (Sprite type, no mipmaps); everything else as a 3D/world texture.
    /// </summary>
    public class TextureImportDefaults : AssetPostprocessor
    {
        public const int MaxSize = 512;
        public const int CrunchQuality = 50;

        void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;

            // Only seed brand-new textures; never stomp existing settings.
            if (!importer.importSettingsMissing)
                return;

            Apply(importer, assetPath);
        }

        internal static void Apply(TextureImporter importer, string path)
        {
            bool isUI = IsUI(path);

            importer.maxTextureSize = MaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = CrunchQuality;
            importer.mipmapEnabled = !isUI;
            importer.anisoLevel = isUI ? 0 : 1;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = isUI ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;

            if (isUI)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
            }

            var webgl = new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = MaxSize,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = true,
                compressionQuality = CrunchQuality,
            };
            importer.SetPlatformTextureSettings(webgl);
        }

        static bool IsUI(string path)
        {
            string p = path.ToLowerInvariant();
            return p.Contains("/ui/") || p.Contains("/sprites/") || p.Contains("/icons/");
        }

        [MenuItem("Tools/Web Build/Reimport Textures With Defaults")]
        static void ReimportAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reimport textures with lean defaults",
                    $"Apply the lean defaults ({MaxSize}px cap, crunch {CrunchQuality}) to EVERY texture " +
                    "under Assets/, overwriting current per-texture settings.\n\nThis can take a while.",
                    "Reimport", "Cancel"))
                return;

            var paths = AssetDatabase.FindAssets("t:Texture2D")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.StartsWith("Assets/"))
                .ToArray();

            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Reimporting textures", paths[i], (float)i / paths.Length))
                        break;

                    if (AssetImporter.GetAtPath(paths[i]) is TextureImporter ti)
                    {
                        Apply(ti, paths[i]);
                        ti.SaveAndReimport();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[TextureImportDefaults] Reimported {paths.Length} textures.");
        }
    }
}
