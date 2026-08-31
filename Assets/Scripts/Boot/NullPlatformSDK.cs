using System;
using UnityEngine;

/// <summary>
/// No-op <see cref="IPlatformSDK"/> for contexts with no web portal: the Editor,
/// standalone builds and tests. Ads resolve to their "no ad" path immediately so
/// calling code keeps flowing; tracking calls are logged only.
/// </summary>
public class NullPlatformSDK : IPlatformSDK
{
    public void Initialize(Action onInitialized = null)
    {
        Debug.Log("[NullPlatformSDK] Initialize (no portal).");
        onInitialized?.Invoke();
    }

    public void GameLoadingStart() { }
    public void GameLoadingFinished() { }

    public bool AdsBlocked() => true;

    public void RequestAd(Action onCompleted, Action onFailed)
    {
        // No portal -> no interstitial; resume immediately, same as a skipped break.
        onCompleted?.Invoke();
    }

    public void RequestRewardedAd(Action onRewarded, Action onFailed)
    {
        // No portal -> a reward can't be granted.
        onFailed?.Invoke();
    }

    public void GameplayStart() => Debug.Log("[NullPlatformSDK] GameplayStart");
    public void GameplayStop() => Debug.Log("[NullPlatformSDK] GameplayStop");
    public void HappyTime() { }
}
