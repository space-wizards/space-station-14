using Content.Shared.DeviceNetwork;

namespace Content.Shared.RoundEnd;

/// <summary>
/// A network payload to broadcast data to shuttle screens.
/// </summary>
public partial record struct ScreenShuttlePayload : INetworkPayload
{
    [DataField]
    public EntityUid? Shuttle;

    [DataField]
    public EntityUid? SourceMap;

    [DataField]
    public EntityUid? DestinationMap;

    [DataField]
    public TimeSpan ShuttleTime;

    [DataField]
    public TimeSpan SourceTime;

    [DataField]
    public TimeSpan DestinationTime;

    [DataField]
    public bool Docked;

    [DataField]
    public string? OverrideText;

    [DataField]
    public Color? OverrideColor;
}
