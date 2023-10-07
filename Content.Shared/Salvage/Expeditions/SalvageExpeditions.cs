using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage.Expeditions;

[Serializable, NetSerializable]
public sealed class SalvageExpeditionConsoleState : BoundUserInterfaceState
{
    public TimeSpan NextOffer;
    public bool Claimed;
    public bool Cooldown;
    public ushort ActiveMission;
    public List<SalvageMissionParams> Missions;

    public SalvageExpeditionConsoleState(TimeSpan nextOffer, bool claimed, bool cooldown, ushort activeMission, List<SalvageMissionParams> missions)
    {
        NextOffer = nextOffer;
        Claimed = claimed;
        Cooldown = cooldown;
        ActiveMission = activeMission;
        Missions = missions;
    }
}

/// <summary>
/// Used to interact with salvage expeditions and claim them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SalvageExpeditionConsoleComponent : Component
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
public sealed partial class SalvageExpeditionDataComponent : Component
{
    /// <summary>
    /// Is there an active salvage expedition.
    /// </summary>
    [ViewVariables]
    public bool Claimed => ActiveMission != 0;

    /// <summary>
    /// Are we actively cooling down from the last salvage mission.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public bool Cooldown = false;

    /// <summary>
    /// Nexy time salvage missions are offered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextOffer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextOffer;

    [ViewVariables]
    public readonly Dictionary<ushort, SalvageMissionParams> Missions = new();

    [ViewVariables] public ushort ActiveMission;

    public ushort NextIndex = 1;
}

[Serializable, NetSerializable]
public sealed record SalvageMissionParams
{
    [ViewVariables]
    public ushort Index;

    [ViewVariables(VVAccess.ReadWrite)] public int Seed;

    public string Difficulty = string.Empty;
}

/// <summary>
/// Created from <see cref="SalvageMissionParams"/>. Only needed for data the client also needs for mission
/// display.
/// </summary>
public sealed record SalvageMission(
    int Seed,
    string Dungeon,
    string Faction,
    string Biome,
    string Air,
    float Temperature,
    Color? Color,
    TimeSpan Duration,
    List<string> Modifiers)
{
    /// <summary>
    /// Seed used for the mission.
    /// </summary>
    public readonly int Seed = Seed;

    /// <summary>
    /// <see cref="SalvageDungeonModPrototype"/> to be used.
    /// </summary>
    public readonly string Dungeon = Dungeon;

    /// <summary>
    /// <see cref="SalvageFactionPrototype"/> to be used.
    /// </summary>
    public readonly string Faction = Faction;

    /// <summary>
    /// Biome to be used for the mission.
    /// </summary>
    public readonly string Biome = Biome;

    /// <summary>
    /// Air mixture to be used for the mission's planet.
    /// </summary>
    public readonly string Air = Air;

    /// <summary>
    /// Temperature of the planet's atmosphere.
    /// </summary>
    public readonly float Temperature = Temperature;

    /// <summary>
    /// Lighting color to be used (AKA outdoor lighting).
    /// </summary>
    public readonly Color? Color = Color;

    /// <summary>
    /// Mission duration.
    /// </summary>
    public TimeSpan Duration = Duration;

    /// <summary>
    /// Modifiers (outside of the above) applied to the mission.
    /// </summary>
    public List<string> Modifiers = Modifiers;
}

[Serializable, NetSerializable]
public enum SalvageConsoleUiKey : byte
{
    Expedition,
}
