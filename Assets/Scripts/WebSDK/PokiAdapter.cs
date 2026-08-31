using System;
using UnityEngine;

public class PokiAdapter : IPlatformSDK
{
    public void Initialize(Action onInitialized = null)
    {
        Debug.Log("[PokiAdapter] Initializing SDK...");

        PokiUnitySDK.Instance.sdkInitializedCallback = () =>
        {
            Debug.Log("[PokiAdapter] SDK Initialized.");

            // NOTE: Poki keeps its loading bar up until gameLoadingFinished() is
            // called. That is now driven by Bootstrapper, once the first playable
            // scene is actually visible - not here on mere SDK-ready.
            onInitialized?.Invoke();
        };

        PokiUnitySDK.Instance.init();
    }

    public bool AdsBlocked()
    {
        // Poki explicitly checks and populates this field during/after initialization
        return PokiUnitySDK.Instance.adblocked;
    }

    public void RequestAd(Action onCompleted, Action onFailed)
    {
        Debug.Log("[PokiAdapter] Requesting Interstitial (Commercial) Ad");

        // Set the callback that fires when the ad finishes (or if it is skipped via AdBlock)
        PokiUnitySDK.Instance.commercialBreakCallBack = () =>
        {
            // For Poki, a commercial break either shows and finishes, or skips if blocked/not ready.
            // Both results mean gameplay should immediately resume, so we use onCompleted.
            onCompleted?.Invoke();
        };

        PokiUnitySDK.Instance.commercialBreak();
    }

    public void RequestRewardedAd(Action onRewarded, Action onFailed)
    {
        Debug.Log("[PokiAdapter] Requesting Rewarded Ad");

        // Set the callback specifically for rewarded results
        PokiUnitySDK.Instance.rewardedBreakCallBack = (withReward) =>
        {
            if (withReward)
            {
                Debug.Log("[PokiAdapter] Rewarded Ad successful!");
                onRewarded?.Invoke();
            }
            else
            {
                Debug.Log("[PokiAdapter] Rewarded Ad failed or was closed early.");
                onFailed?.Invoke();
            }
        };

        PokiUnitySDK.Instance.rewardedBreak();
    }

    public void GameplayStart()
    {
        Debug.Log("[PokiAdapter] Gameplay Start Tracking");
        PokiUnitySDK.Instance.gameplayStart();
    }

    public void GameplayStop()
    {
        Debug.Log("[PokiAdapter] Gameplay Stop Tracking");
        PokiUnitySDK.Instance.gameplayStop();
    }

    public void HappyTime()
    {
        // No Poki event
    }

    // ------------------------------------------------------------------------
    // Poki-specific features below. These fall outside the generic
    // IPlatformSDK contract, but are exposed directly since Poki is this
    // template's only supported platform.
    // ------------------------------------------------------------------------

    /// <summary>Show Poki's loading indicator again for extra mid-game loading (e.g. a new level).</summary>
    public void GameLoadingStart() => PokiUnitySDK.Instance.gameLoadingStart();

    /// <summary>Call once the extra mid-game loading started via GameLoadingStart() has finished.</summary>
    public void GameLoadingFinished() => PokiUnitySDK.Instance.gameLoadingFinished();

    public void GetUser(Action<PokiUser> onResolved, Action onRejected)
    {
        PokiUnitySDK.Instance.getUserResolvedCallback = (user) => onResolved?.Invoke(user);
        PokiUnitySDK.Instance.getUserRejectedCallback = () => onRejected?.Invoke();
        PokiUnitySDK.Instance.getUser();
    }

    public void GetToken(Action<string> onResolved, Action onRejected)
    {
        PokiUnitySDK.Instance.getTokenResolvedCallback = (token) => onResolved?.Invoke(token);
        PokiUnitySDK.Instance.getTokenRejectedCallback = () => onRejected?.Invoke();
        PokiUnitySDK.Instance.getToken();
    }

    public void Login(Action onResolved, Action onRejected)
    {
        PokiUnitySDK.Instance.loginResolvedCallback = () => onResolved?.Invoke();
        PokiUnitySDK.Instance.loginRejectedCallback = () => onRejected?.Invoke();
        PokiUnitySDK.Instance.login();
    }

    public void ShareableURL(ScriptableObject urlParams, Action<string> onResolved, Action onRejected)
    {
        PokiUnitySDK.Instance.shareableURLResolvedCallback = (url) => onResolved?.Invoke(url);
        PokiUnitySDK.Instance.shareableURLRejectedCallback = () => onRejected?.Invoke();
        PokiUnitySDK.Instance.shareableURL(urlParams);
    }

    public void CustomEvent(string noun, string verb, ScriptableObject data = null) =>
        PokiUnitySDK.Instance.customEvent(noun, verb, data);

    public void Measure(string category, string what = "", string action = "") =>
        PokiUnitySDK.Instance.measure(category, what, action);

    public void DisplayAd(string identifier, string size, string top, string left) =>
        PokiUnitySDK.Instance.displayAd(identifier, size, top, left);

    public void DestroyAd(string identifier) => PokiUnitySDK.Instance.destroyAd(identifier);

    public void MovePill(double topPercent, double topPx) => PokiUnitySDK.Instance.movePill(topPercent, topPx);

    public string GetURLParam(string name) => PokiUnitySDK.Instance.getURLParam(name);

    public string GetLanguage() => PokiUnitySDK.Instance.getLanguage();

    public string OpenExternalLink(string link) => PokiUnitySDK.Instance.openExternalLink(link);

    public void Redirect(string destination) => PokiUnitySDK.Instance.redirect(destination);

    public void LogError(string error) => PokiUnitySDK.Instance.logError(error);
}
