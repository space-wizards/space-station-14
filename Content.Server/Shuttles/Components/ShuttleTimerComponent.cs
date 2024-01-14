namespace Content.Server.Shuttles.Components;

[RegisterComponent]
public sealed partial class ShuttleTimerComponent : Component
{

}

/// <summary>
///     Awkward hashable string consts because NetworkPayload requires string keys
///     TODO: Refactor NetworkPayload to accept bytes from enums?
/// </summary>
public sealed class ShuttleTimerMasks
{
    public static readonly string ShuttleTime = "ShuttleTime";
    public static readonly string DestTime = "DestTime";
    public static readonly string SourceTime = "SourceTime";
    public static readonly string ShuttleMap = "ShuttleMap";
    public static readonly string SourceMap = "SourceMap";
    public static readonly string DestMap = "DestMap";
    public static readonly string Docked = "Docked";
    public static readonly string Text = "Text";
}

