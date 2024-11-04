using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Shared._Starlight.Antags.Abductor;
[Serializable, NetSerializable]
public enum AbductorCameraConsoleUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class AbductorCameraConsoleBuiState : BoundUserInterfaceState
{
    public required Dictionary<int, StationBeacons> Stations { get; init; }
}

[Serializable, NetSerializable]
public sealed class StationBeacons
{
    public required int StationId { get; init; }
    public required string Name { get; init; }
    public required List<NavMapBeacon> Beacons { get; init; }
}
[Serializable, NetSerializable]
public sealed class AbductorBeaconChosenBuiMsg : BoundUserInterfaceMessage
{
    public required NavMapBeacon Beacon { get; init; }
}
