/// <summary>
/// Process-wide handle to the active platform SDK. Assigned once by
/// <see cref="Bootstrapper"/> before the first playable scene loads, then read by
/// gameplay code, e.g. <c>Game.Platform.GameplayStart()</c>.
/// </summary>
public static class Game
{
    public static IPlatformSDK Platform { get; set; }
}
