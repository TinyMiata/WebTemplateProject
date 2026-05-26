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
}