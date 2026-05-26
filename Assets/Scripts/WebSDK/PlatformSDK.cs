using System;

public interface IPlatformSDK
{
    // SDK Initialization with callback for async operations
    void Initialize(Action onInitialized = null);

    // Some SDKs like Poki can check if ads are blocked directly
    bool AdsBlocked();

    // Ads
    void RequestAd(Action onCompleted, Action onFailed);
    void RequestRewardedAd(Action onRewarded, Action onFailed);

    // Gameplay Tracking (Important for Web SDKs)
    void GameplayStart();
    void GameplayStop();

    // Custom events
    void HappyTime();
}