using System;
using UnityEngine;
using CrazyGames;

public class CrazyGamesAdapter : IPlatformSDK
{
    public void Initialize(Action onInitialized = null)
    {
        Debug.Log("[CrazyGamesAdapter] Initializing SDK...");

        CrazySDK.Init(() =>
        {
            Debug.Log("[CrazyGamesAdapter] SDK Initialized.");
            onInitialized?.Invoke();
        });
    }

    public bool AdsBlocked()
    {
        // CrazyGames handles ad-blocks automatically by immediately triggering 
        // the error callbacks. Return false so ads are always requested.
        return false;
    }

    public void RequestAd(Action onCompleted, Action onFailed)
    {
        Debug.Log("[CrazyGamesAdapter] Requesting Interstitial (Midgame) Ad");

        CrazySDK.Ad.RequestAd(
            CrazyAdType.Midgame,
            () => {},
            (error) => { onFailed?.Invoke(); },
            () => { onCompleted?.Invoke(); }
        );
    }

    public void RequestRewardedAd(Action onRewarded, Action onFailed)
    {
        Debug.Log("[CrazyGamesAdapter] Requesting Rewarded Ad");

        CrazySDK.Ad.RequestAd(
            CrazyAdType.Rewarded,
            () => {},
            (error) => { onFailed?.Invoke(); },
            () => { onRewarded?.Invoke(); }
        );
    }

    public void GameplayStart()
    {
        Debug.Log("[CrazyGamesAdapter] Gameplay Start Tracking");

        if (CrazySDK.IsInitialized)
            CrazySDK.Game.GameplayStart();
    }

    public void GameplayStop()
    {
        Debug.Log("[CrazyGamesAdapter] Gameplay Stop Tracking");

        if (CrazySDK.IsInitialized)
            CrazySDK.Game.GameplayStop();
    }

    public void HappyTime()
    {
        Debug.Log("[CrazyGamesAdapter] Happy Time!");

        if (CrazySDK.IsInitialized)
            CrazySDK.Game.HappyTime();
    }
}