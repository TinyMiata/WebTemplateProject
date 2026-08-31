/// <summary>
/// Selects the <see cref="IPlatformSDK"/> implementation for the current build:
/// the real Poki adapter only in an actual WebGL player, a no-op everywhere else.
/// </summary>
public static class PlatformFactory
{
    public static IPlatformSDK Create()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new PokiAdapter();
#else
        return new NullPlatformSDK();
#endif
    }
}
