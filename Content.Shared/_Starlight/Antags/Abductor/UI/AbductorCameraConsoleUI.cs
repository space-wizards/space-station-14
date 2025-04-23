using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Shared.Starlight.Antags.Abductor;

[Serializable, NetSerializable]
public sealed class AbductorCameraConsoleBuiState : BoundUserInterfaceState
{
    public required Dictionary<int, StationBeacons> Stations { get; init; }
}

[Serializable, NetSerializable]
public sealed class AbductorConsoleBuiState : BoundUserInterfaceState
{
    public NetEntity? Target { get; init; }
    public string? TargetName { get; init; }
    public string? VictimName { get; init; }
    public bool AlienPadFound { get; init; }
    public bool ExperimentatorFound { get; init; }
    public bool DispencerFound { get; init; }
    public bool ArmorFound { get; init; }
    public bool ArmorLocked { get; init; }
    public AbductorArmorModeType CurrentArmorMode { get; init; }
    public int? CurrentBalance { get; init; }
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
[Serializable, NetSerializable]
public sealed class AbductorAttractBuiMsg : BoundUserInterfaceMessage
{
}
[Serializable, NetSerializable]
public sealed class AbductorCompleteExperimentBuiMsg : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AbductorVestModeChangeBuiMsg : BoundUserInterfaceMessage
{
    public required AbductorArmorModeType Mode { get; init; }
}

[Serializable, NetSerializable]
public sealed class AbductorItemBuyedBuiMsg : BoundUserInterfaceMessage
{
    public required EntProtoId Item { get; init; }
    public required int Price { get; init; }
}

[Serializable, NetSerializable]
public sealed class AbductorLockBuiMsg : BoundUserInterfaceMessage
{
}