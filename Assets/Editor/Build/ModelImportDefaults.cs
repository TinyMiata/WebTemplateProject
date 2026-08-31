using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WebTemplate.EditorTools
{
    /// <summary>
    /// Every imported <c>.fbx</c> comes in mesh + rig only — no materials are
    /// generated or extracted (no stray <c>.mat</c> files), and every renderer
    /// slot is explicitly pointed at Unity's built-in <c>Default-Material</c>
    /// instead of being left empty.
    ///
    /// Applies to .fbx on every import. Other model formats (.obj/.blend/.dae)
    /// are left alone.
    /// </summary>
    public class ModelImportDefaults : AssetPostprocessor
    {
        static bool IsFbx(string path) =>
            path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

        static Material DefaultMaterial =>
            AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

        // --- Import graph: never create material assets for an .fbx ------------
        void OnPreprocessModel()
        {
            if (!IsFbx(assetPath))
                return;

            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;

            // Drop any material remaps a previous import left in the .meta.
            foreach (var id in mi.GetExternalObjectMap()
                                 .Keys
                                 .Where(k => k.type == typeof(Material))
                                 .ToArray())
                mi.RemoveRemap(id);
        }

        // --- After the hierarchy is built: pin every slot to Default-Material -
        void OnPostprocessModel(GameObject root)
        {
            if (!IsFbx(assetPath))
                return;

            var def = DefaultMaterial;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                // Keep the array length (== submesh count); just fill every slot.
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = def;
                r.sharedMaterials = mats;
            }
        }

        [MenuItem("Tools/Web Build/Reimport FBX (mesh only, Default-Material)")]
        static void ReimportAllFbx()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reimport all FBX",
                    "Set Material Creation Mode = None and assign Default-Material to " +
                    "every renderer slot on every .fbx under Assets/, then reimport.",
                    "Reimport", "Cancel"))
                return;

            var paths = AssetDatabase.FindAssets("t:Model")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.StartsWith("Assets/") && IsFbx(p))
                .ToArray();

            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Reimporting FBX", paths[i], (float)i / paths.Length);
                    if (AssetImporter.GetAtPath(paths[i]) is ModelImporter mi)
                    {
                        mi.materialImportMode = ModelImporterMaterialImportMode.None;
                        foreach (var id in mi.GetExternalObjectMap().Keys
                                             .Where(k => k.type == typeof(Material)).ToArray())
                            mi.RemoveRemap(id);
                        mi.SaveAndReimport();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Debug.Log($"[ModelImportDefaults] Reimported {paths.Length} .fbx file(s).");
            }
        }
    }
}
