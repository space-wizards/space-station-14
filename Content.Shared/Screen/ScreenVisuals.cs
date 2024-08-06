using Robust.Shared.Serialization;

namespace Content.Shared.Screen;

/// <summary>
///     Legacy byte facilitating appearancesystem updates. First target for a refactor. See more in the client ScreenSystem
/// </summary>
[Serializable, NetSerializable]
public enum ScreenVisuals : byte
{
    Update
}

/// <summary>
///     ScreenUpdates with lower ScreenPriority values display over others.
/// </summary>
[Serializable, NetSerializable]
public enum ScreenPriority : byte
{
    Nuke,
    Brig, // brig only has to outprioritize Default. nuke updates are on a different frequency

    Shuttle,
    Default
}

/// <summary>
///     Player-facing hashable consts for NetworkPayloads
/// </summary>
[Serializable, NetSerializable]
public sealed class ScreenMasks
{
    // main updates dict
    public static readonly string Updates = Loc.GetString("screen-updates");

    // shuttle timer accompanying text
    public static readonly string ETA = Loc.GetString("screen-eta");
    public static readonly string ETD = Loc.GetString("screen-etd");
    public static readonly string Bye = Loc.GetString("screen-bye");
    public static readonly string Kill = Loc.GetString("screen-kill");

    // nuke timer accompanying text
    public static readonly string Nuke = Loc.GetString("screen-nuke");
}

/// <summary>
///     A ScreenUpdate is a thing shown on a screen. Right now it is only text or a timer.
///     ScreenUpdates get passed by the server into a client ScreenComponent's "Updates" SortedDict,
///     which only displays the highest priority update.
/// </summary>
/// TODO: Include small images/videos later; ScreenTimerComponent -> "DynamicScreenUpdateComponent".
[Serializable, NetSerializable]
public struct ScreenUpdate
{
    public NetEntity? Subnet { get; }
    public ScreenPriority Priority { get; }
    public string? Text { get; }
    public TimeSpan? Timer { get; }
    public Color? Color { get; }

    public ScreenUpdate(NetEntity? subnet, ScreenPriority priority, string? text = null, TimeSpan? timer = null, Color? color = null)
    {
        Subnet = subnet; Priority = priority; Text = text; Timer = timer; Color = color;
    }
}
