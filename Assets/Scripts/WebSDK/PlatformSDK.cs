using System;

public interface IPlatformSDK
{
    // SDK Initialization with callback for async operations
    void Initialize(Action onInitialized = null);

    // Portal loading indicator. GameLoadingFinished() dismisses the portal's own
    // loading screen - call it once the first playable scene is actually visible.
    // Call the Start/Finished pair again around later heavy loads (e.g. a level).
    void GameLoadingStart();
    void GameLoadingFinished();

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