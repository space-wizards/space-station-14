using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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

    /// <summary>
    /// Has the mission been completed.
    /// </summary>
    public bool MissionCompleted;

    public ushort NextIndex = 1;
}

[Serializable, NetSerializable]
public sealed class SalvageMission
{
    [ViewVariables]
    public ushort Index;

    [ViewVariables(VVAccess.ReadWrite), DataField("config", required: true)]
    public string Config = default!;

    [ViewVariables] public TimeSpan Duration;

    [ViewVariables] public int Seed;

    // TODO: Environment mods

    // TODO: Hazard pay
}

[Serializable, NetSerializable]
public enum SalvageEnvironment : byte
{
    Invalid = 0,
    Caves,
}

[Serializable, NetSerializable]
public enum SalvageConsoleUiKey : byte
{
    Expedition,
}
