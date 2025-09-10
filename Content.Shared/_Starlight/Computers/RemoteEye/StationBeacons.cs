using Robust.Shared.Serialization;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Shared._Starlight.Computers.RemoteEye;

[Serializable, NetSerializable]
public sealed class StationBeacons
{
    public required int StationId { get; init; }
    public required string Name { get; init; }
    public required List<NavMapBeacon> Beacons { get; init; }
}