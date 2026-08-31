using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The single runtime entry point. Lives in the tiny Bootstrap scene (the only
/// scene in the build). It:
///   1. picks + initialises the platform SDK,
///   2. initialises Addressables,
///   3. streams in the first playable scene behind a progress bar,
///   4. dismisses the portal loading screen and unloads itself.
///
/// Keeping every real asset out of the build scene list is what keeps
/// <c>webgl.data</c> small; this class is the hand-off that makes that work.
/// </summary>
public class Bootstrapper : MonoBehaviour
{
    [Tooltip("Addressables key (address) of the first scene to load.")]
    [SerializeField] string firstSceneKey = "Main";

    [Tooltip("Optional fill bar (Image, type = Filled). Shown only for local / non-portal runs.")]
    [SerializeField] Image fillBar;

    async void Start()
    {
        // Capture the SDK locally: this component is destroyed when the Bootstrap
        // scene unloads at the end, so the final calls must not touch `this`.
        var platform = PlatformFactory.Create();
        Game.Platform = platform;

        SetProgress(0f);

        try
        {
            // 1. Addressables content catalog.
            await Addressables.InitializeAsync(false).Task;

            // 2. Portal SDK (Poki init(); no-portal builds resolve immediately).
            var sdkReady = new TaskCompletionSource<bool>();
            platform.Initialize(() => sdkReady.TrySetResult(true));
            await sdkReady.Task;

            // 3. Pre-download the first scene's bundles (progress 0 -> 0.9).
            var download = Addressables.DownloadDependenciesAsync(firstSceneKey, false);
            while (!download.IsDone)
            {
                var status = download.GetDownloadStatus();
                SetProgress((status.TotalBytes > 0 ? status.Percent : 0f) * 0.9f);
                await Task.Yield();
            }
            Addressables.Release(download);

            // 4. Load + activate the first scene additively (progress 0.9 -> 1.0).
            var load = Addressables.LoadSceneAsync(firstSceneKey, LoadSceneMode.Additive);
            while (!load.IsDone)
            {
                SetProgress(0.9f + load.PercentComplete * 0.1f);
                await Task.Yield();
            }
            SetProgress(1f);

            if (load.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[Bootstrapper] Failed to load scene '{firstSceneKey}'. " +
                    "Is it marked Addressable with that address?");
                return;
            }

            SceneManager.SetActiveScene(load.Result.Scene);

            // 5. Hand off: dismiss the portal loading screen, drop the boot scene.
            platform.GameLoadingFinished();
            await SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void SetProgress(float t)
    {
        if (fillBar != null)
            fillBar.fillAmount = Mathf.Clamp01(t);
    }
}
