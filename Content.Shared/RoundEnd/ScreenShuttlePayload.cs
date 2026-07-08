using Content.Shared.DeviceNetwork;

namespace Content.Shared.RoundEnd;

public sealed partial class ScreenShuttlePayload : NetworkPayloadBase<ScreenShuttlePayload>
{
    [DataField]
    public NetEntity? Shuttle;

    [DataField]
    public NetEntity? SourceMap;

    [DataField]
    public NetEntity? DestinationMap;

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
