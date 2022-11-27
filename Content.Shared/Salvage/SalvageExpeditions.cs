using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage;

[Serializable, NetSerializable]
public sealed class SalvageExpeditionConsoleState : BoundUserInterfaceState
{
    public bool Claimed;
    public ushort ActiveMission;
    public List<SalvageMission> Missions;

    public SalvageExpeditionConsoleState(bool claimed, ushort activeMission, List<SalvageMission> missions)
    {
        Claimed = claimed;
        ActiveMission = activeMission;
        Missions = missions;
    }
}

/// <summary>
/// Used to interact with salvage expeditions and claim them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class SalvageExpeditionConsoleComponent : Component
{

}

[Serializable, NetSerializable]
public sealed class ClaimSalvageMessage : BoundUserInterfaceMessage
{
    public ushort Index;
}


/// <summary>
/// Added per station to store data on their available salvage missions.
/// </summary>
[RegisterComponent]
public sealed class SalvageExpeditionDataComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Is there an active salvage expedition.
    /// </summary>
    [ViewVariables]
    public bool Claimed => ActiveMission != 0;

    /// <summary>
    /// Nexy time salvage missions are offered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextOffer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextOffer;

    [ViewVariables]
    public readonly Dictionary<ushort, SalvageMission> Missions = new();

    [ViewVariables] public ushort ActiveMission;

    public ushort NextIndex = 1;
}

[Serializable, NetSerializable]
public sealed class SalvageMission
{
    [ViewVariables]
    public ushort Index;

    [ViewVariables]
    public SalvageMissionType MissionType = SalvageMissionType.Invalid;

    [ViewVariables]
    public SalvageEnvironment Environment = SalvageEnvironment.Invalid;

    [ViewVariables] public TimeSpan Duration;

    [ViewVariables] public int Seed;

    // TODO: Config

    // TODO: Environment modifiers

    // TODO: Hazard pay
}

[Serializable, NetSerializable]
public enum SalvageEnvironment : byte
{
    Invalid = 0,
    Caves,
}

[Serializable, NetSerializable]
public enum SalvageMissionType : byte
{
    Invalid = 0,

    /// <summary>
    /// Destroy specific structures.
    /// </summary>
    Structure,
}

[Serializable, NetSerializable]
public enum SalvageConsoleUiKey : byte
{
    Expedition,
}
