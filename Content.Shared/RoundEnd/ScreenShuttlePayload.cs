using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.RoundEnd;

[Serializable, NetSerializable]
public sealed partial class ScreenShuttlePayload : HandledNetworkPayload
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
