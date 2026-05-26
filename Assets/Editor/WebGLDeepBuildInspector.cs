using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class WebGLDeepBuildInspector : EditorWindow
{
    Vector2 scroll;

    class Entry
    {
        public string name;
        public long size;
    }

    List<Entry> projectAssets = new();
    List<Entry> packages = new();

    long projectSize;

    ListRequest packageRequest;

    [MenuItem("Tools/WebGL Deep Build Inspector")]
    static void Open()
    {
        GetWindow<WebGLDeepBuildInspector>("WebGL Deep Inspector");
    }

    void OnGUI()
    {
        GUILayout.Label("Unity WebGL Build Size Analyzer", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan Project + Packages"))
        {
            ScanAssets();
            ScanPackages();
        }

        scroll = GUILayout.BeginScrollView(scroll);

        DrawSettings();
        DrawSection("Largest Project Assets", projectAssets);
        DrawSection("Installed Packages", packages);

        GUILayout.Space(10);
        GUILayout.Label("Approx Project Asset Size: " + Format(projectSize));

        GUILayout.EndScrollView();
    }

    void ScanAssets()
    {
        projectAssets.Clear();
        projectSize = 0;

        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets"))
                continue;

            try
            {
                var info = new FileInfo(path);
                projectSize += info.Length;

                projectAssets.Add(new Entry
                {
                    name = path,
                    size = info.Length
                });
            }
            catch { }
        }

        projectAssets = projectAssets
            .OrderByDescending(e => e.size)
            .Take(20)
            .ToList();
    }

    void ScanPackages()
    {
        packages.Clear();

        packageRequest = Client.List(true);
        EditorApplication.update += PackageProgress;
    }

    void PackageProgress()
    {
        if (!packageRequest.IsCompleted)
            return;

        EditorApplication.update -= PackageProgress;

        if (packageRequest.Status == StatusCode.Success)
        {
            foreach (var package in packageRequest.Result)
            {
                long size = EstimatePackageSize(package.resolvedPath);

                packages.Add(new Entry
                {
                    name = package.name,
                    size = size
                });
            }

            packages = packages
                .OrderByDescending(p => p.size)
                .ToList();
        }

        Repaint();
    }

    long EstimatePackageSize(string path)
    {
        if (!Directory.Exists(path))
            return 0;

        long total = 0;

        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

        foreach (var f in files)
        {
            try
            {
                total += new FileInfo(f).Length;
            }
            catch { }
        }

        return total;
    }

    void DrawSection(string title, List<Entry> entries)
    {
        GUILayout.Space(10);
        GUILayout.Label(title, EditorStyles.boldLabel);

        foreach (var e in entries)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Format(e.size), GUILayout.Width(90));
            GUILayout.Label(e.name);
            GUILayout.EndHorizontal();
        }
    }

    void DrawSettings()
    {
        GUILayout.Space(10);
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);

        DrawCheck(
            "Compression = Brotli",
            PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli
        );

        DrawCheck(
            "Strip Engine Code",
            PlayerSettings.stripEngineCode
        );

        DrawCheck(
            "Managed Stripping = High",
            PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL) == ManagedStrippingLevel.High
        );

        DrawCheck(
            "Exceptions Disabled",
            PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.None
        );

        DrawCheck(
            "Development Build Disabled",
            !EditorUserBuildSettings.development
        );
    }

    void DrawCheck(string label, bool ok)
    {
        GUILayout.BeginHorizontal();

        GUI.color = ok ? Color.green : Color.yellow;
        GUILayout.Label(ok ? "✓" : "⚠", GUILayout.Width(20));
        GUI.color = Color.white;

        GUILayout.Label(label);

        GUILayout.EndHorizontal();
    }

    string Format(long bytes)
    {
        float mb = bytes / (1024f * 1024f);
        if (mb >= 1)
            return mb.ToString("0.00") + " MB";

        float kb = bytes / 1024f;
        if (kb >= 1)
            return kb.ToString("0.00") + " KB";

        return bytes + " B";
    }
}