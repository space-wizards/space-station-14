using Robust.Shared.Serialization;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Shared._Starlight.Computers.RemoteEye;

[Serializable, NetSerializable]
public sealed class BeaconChosenBuiMsg : BoundUserInterfaceMessage
{
    public required NavMapBeacon Beacon { get; init; }
}