using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Feeds the browser's CSS env(safe-area-inset-*) values into
/// <see cref="SafeArea.Override"/> on WebGL, where <see cref="Screen.safeArea"/>
/// is otherwise always the full canvas.
///
/// Put this on the same GameObject as <see cref="SafeArea"/>. No-op on every
/// non-WebGL platform and in the Editor (use the Device Simulator there).
/// </summary>
[DisallowMultipleComponent]
public class WebSafeArea : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern float SafeAreaBridge_Inset(int edge);

    int _lastW, _lastH;
    float _pollTimer;

    void OnEnable()
    {
        Refresh();
    }

    void OnDisable()
    {
        SafeArea.Override = null;
    }

    void Update()
    {
        // Resize covers rotation and browser-UI show/hide. Also re-poll on a
        // slow timer for the first couple of seconds, since iOS Safari reports
        // insets a frame or two late on load.
        _pollTimer += Time.unscaledDeltaTime;

        if (Screen.width != _lastW || Screen.height != _lastH || _pollTimer < 2f)
            Refresh();
    }

    void Refresh()
    {
        _lastW = Screen.width;
        _lastH = Screen.height;

        float w = Screen.width;
        float h = Screen.height;
        if (w <= 0f || h <= 0f)
            return;

        float top    = Mathf.Clamp(SafeAreaBridge_Inset(0), 0f, h * 0.5f);
        float right  = Mathf.Clamp(SafeAreaBridge_Inset(1), 0f, w * 0.5f);
        float bottom = Mathf.Clamp(SafeAreaBridge_Inset(2), 0f, h * 0.5f);
        float left   = Mathf.Clamp(SafeAreaBridge_Inset(3), 0f, w * 0.5f);

        // Screen.safeArea origin is bottom-left.
        SafeArea.Override = new Rect(left, bottom, w - left - right, h - top - bottom);
    }
#endif
}
