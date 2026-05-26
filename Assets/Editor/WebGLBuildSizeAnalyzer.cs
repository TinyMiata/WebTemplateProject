using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class WebGLBuildSizeInspector : EditorWindow
{
    Vector2 scroll;

    class AssetEntry
    {
        public string path;
        public long size;
        public Object asset;
    }

    List<AssetEntry> textures = new();
    List<AssetEntry> audio = new();
    List<AssetEntry> meshes = new();

    long totalSize;

    [MenuItem("Tools/WebGL Build Size Inspector")]
    public static void Open()
    {
        GetWindow<WebGLBuildSizeInspector>("WebGL Size Inspector");
    }

    void OnGUI()
    {
        GUILayout.Label("WebGL Build Size Inspector", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan Project"))
        {
            Scan();
        }

        scroll = GUILayout.BeginScrollView(scroll);

        DrawSettingsCheck();
        DrawAssets("Largest Textures", textures);
        DrawAssets("Largest Audio Files", audio);
        DrawAssets("Largest Meshes", meshes);

        GUILayout.Space(20);
        GUILayout.Label($"Approx Raw Asset Size: {FormatSize(totalSize)}");

        GUILayout.EndScrollView();
    }

    void Scan()
    {
        textures.Clear();
        audio.Clear();
        meshes.Clear();
        totalSize = 0;

        string[] assets = AssetDatabase.GetAllAssetPaths();

        foreach (var path in assets)
        {
            if (!path.StartsWith("Assets"))
                continue;

            long size = 0;

            try
            {
                size = new FileInfo(path).Length;
            }
            catch
            {
                continue;
            }

            totalSize += size;

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (obj is Texture)
                textures.Add(new AssetEntry { path = path, size = size, asset = obj });

            if (obj is AudioClip)
                audio.Add(new AssetEntry { path = path, size = size, asset = obj });

            if (obj is Mesh)
                meshes.Add(new AssetEntry { path = path, size = size, asset = obj });
        }

        textures = textures.OrderByDescending(t => t.size).Take(20).ToList();
        audio = audio.OrderByDescending(t => t.size).Take(20).ToList();
        meshes = meshes.OrderByDescending(t => t.size).Take(20).ToList();
    }

    void DrawAssets(string title, List<AssetEntry> list)
    {
        GUILayout.Space(10);
        GUILayout.Label(title, EditorStyles.boldLabel);

        if (list.Count == 0)
        {
            GUILayout.Label("No assets found or scan not run.");
            return;
        }

        foreach (var entry in list)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = entry.asset;
                EditorGUIUtility.PingObject(entry.asset);
            }

            GUILayout.Label(FormatSize(entry.size), GUILayout.Width(80));
            GUILayout.Label(entry.path);

            GUILayout.EndHorizontal();
        }
    }

    void DrawSettingsCheck()
    {
        GUILayout.Space(10);
        GUILayout.Label("Build Settings Check", EditorStyles.boldLabel);

        CheckSetting(
            "Compression Format = Brotli",
            PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli
        );

        CheckSetting(
            "Exceptions Disabled",
            PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.None
        );

        CheckSetting(
            "Strip Engine Code Enabled",
            PlayerSettings.stripEngineCode
        );

        CheckSetting(
            "Managed Stripping = High",
            PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL) == ManagedStrippingLevel.High
        );

        CheckSetting(
            "Development Build Disabled",
            !EditorUserBuildSettings.development
        );
    }

    void CheckSetting(string label, bool good)
    {
        GUILayout.BeginHorizontal();

        GUI.color = good ? Color.green : Color.yellow;
        GUILayout.Label(good ? "✓" : "⚠", GUILayout.Width(20));
        GUI.color = Color.white;

        GUILayout.Label(label);

        GUILayout.EndHorizontal();
    }

    string FormatSize(long bytes)
    {
        float mb = bytes / (1024f * 1024f);
        if (mb >= 1f) return mb.ToString("0.00") + " MB";

        float kb = bytes / 1024f;
        if (kb >= 1f) return kb.ToString("0.00") + " KB";

        return bytes + " B";
    }
}