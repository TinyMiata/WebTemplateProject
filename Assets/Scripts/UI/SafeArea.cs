using UnityEngine;

/// <summary>
/// Drives this RectTransform to match the device safe area (notches, rounded
/// corners, home indicators, browser UI insets).
///
/// Usage: put this on a full-screen stretched child of the Canvas root and
/// parent all other UI under it. The Canvas keeps handling DPI via CanvasScaler;
/// this only handles insets.
///
/// Re-applies whenever the safe area, screen size, or orientation changes, so it
/// works with runtime rotation and Unity's Device Simulator.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeArea : MonoBehaviour
{
    [Header("Edges to inset")]
    [SerializeField] bool left = true;
    [SerializeField] bool right = true;
    [SerializeField] bool top = true;
    [SerializeField] bool bottom = true;

    [Header("Extra padding (px, inside the safe area)")]
    [SerializeField, Min(0f)] float padding = 0f;

    /// <summary>
    /// Optional override in screen pixels. When set (width &gt; 0 &amp;&amp; height &gt; 0)
    /// this is used instead of <see cref="Screen.safeArea"/> — e.g. fed from the
    /// WebGL template's CSS env(safe-area-inset-*) values.
    /// </summary>
    public static Rect? Override;

    RectTransform _rt;
    Rect _lastArea;
    int _lastW, _lastH;
    ScreenOrientation _lastOrientation;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        Rect area = CurrentArea();
        if (area == _lastArea &&
            Screen.width == _lastW &&
            Screen.height == _lastH &&
            Screen.orientation == _lastOrientation)
            return;

        Apply();
    }

    static Rect CurrentArea()
    {
        if (Override.HasValue && Override.Value.width > 0f && Override.Value.height > 0f)
            return Override.Value;
        return Screen.safeArea;
    }

    void Apply()
    {
        if (_rt == null)
            return;

        int w = Screen.width;
        int h = Screen.height;
        if (w <= 0 || h <= 0)
            return;

        Rect area = CurrentArea();

        float xMin = left ? area.xMin + padding : 0f;
        float yMin = bottom ? area.yMin + padding : 0f;
        float xMax = right ? area.xMax - padding : w;
        float yMax = top ? area.yMax - padding : h;

        // Never let padding invert the rect on a small screen.
        if (xMax <= xMin) { xMin = area.xMin; xMax = area.xMax; }
        if (yMax <= yMin) { yMin = area.yMin; yMax = area.yMax; }

        Vector2 anchorMin = new(xMin / w, yMin / h);
        Vector2 anchorMax = new(xMax / w, yMax / h);

        if (float.IsNaN(anchorMin.x) || float.IsNaN(anchorMin.y) ||
            float.IsNaN(anchorMax.x) || float.IsNaN(anchorMax.y))
            return;

        _rt.anchorMin = new Vector2(Mathf.Clamp01(anchorMin.x), Mathf.Clamp01(anchorMin.y));
        _rt.anchorMax = new Vector2(Mathf.Clamp01(anchorMax.x), Mathf.Clamp01(anchorMax.y));
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;

        _lastArea = area;
        _lastW = w;
        _lastH = h;
        _lastOrientation = Screen.orientation;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying && _rt != null)
            Apply();
    }
#endif
}
