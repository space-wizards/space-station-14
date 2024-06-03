namespace Content.Server.Screens.Components;

[RegisterComponent]
public sealed partial class ScreenComponent : Component
{

}

/// <summary>
///     Player-facing hashable string consts for NetworkPayload
/// </summary>
public sealed class ScreenMasks
{
    public static readonly string Text = Loc.GetString("screen-text");
    public static readonly string Color = Loc.GetString("screen-color");
}

/// <summary>
///     Player-facing hashable string consts for NetworkPayload
/// </summary>
public sealed class ShuttleTimerMasks
{
    public static readonly string ShuttleTime = Loc.GetString("shuttle-timer-shuttle-time");
    public static readonly string DestTime = Loc.GetString("shuttle-timer-dest-time");
    public static readonly string SourceTime = Loc.GetString("shuttle-timer-source-time");
    public static readonly string ShuttleMap = Loc.GetString("shuttle-timer-shuttle-map");
    public static readonly string SourceMap = Loc.GetString("shuttle-timer-source-map");
    public static readonly string DestMap = Loc.GetString("shuttle-timer-dest-map");
    public static readonly string Docked = Loc.GetString("shuttle-timer-docked");
    public static readonly string ETA = Loc.GetString("shuttle-timer-eta");
    public static readonly string ETD = Loc.GetString("shuttle-timer-etd");
    public static readonly string Bye = Loc.GetString("shuttle-timer-bye");
    public static readonly string Kill = Loc.GetString("shuttle-timer-kill");
}

