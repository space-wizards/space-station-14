using Content.Shared.Parallax.Biomes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

[Serializable, NetSerializable]
public sealed class SalvageExpeditionConsoleState : BoundUserInterfaceState
{
    public TimeSpan NextOffer;
    public bool Claimed;
    public ushort ActiveMission;
    public List<SalvageMissionParams> Missions;

    public SalvageExpeditionConsoleState(TimeSpan nextOffer, bool claimed, ushort activeMission, List<SalvageMissionParams> missions)
    {
        NextOffer = nextOffer;
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
    public readonly Dictionary<ushort, SalvageMissionParams> Missions = new();

    [ViewVariables] public ushort ActiveMission;

    public ushort NextIndex = 1;
}

[Serializable, NetSerializable]
public sealed record SalvageMissionParams
{
    [ViewVariables]
    public ushort Index;

    [ViewVariables(VVAccess.ReadWrite), DataField("config", required: true, customTypeSerializer:typeof(SalvageMissionPrototype))]
    public string Config = default!;

    [ViewVariables(VVAccess.ReadWrite)] public int Seed;

    [ViewVariables(VVAccess.ReadWrite)] public DifficultyRating Difficulty;
}

/// <summary>
/// Created from <see cref="SalvageMissionParams"/>. Only needed for data the client also needs for mission
/// display.
/// </summary>
public sealed record SalvageMission(
    int Seed,
    string Dungeon,
    string Faction,
    string Mission,
    string? Biome,
    Color? Color,
    TimeSpan Duration)
{
    public readonly int Seed = Seed;

    public readonly string Dungeon = Dungeon;

    public readonly string Faction = Faction;

    public readonly string Mission = Mission;

    public readonly string? Biome = Biome;

    public readonly Color? Color = Color;

    public TimeSpan Duration = Duration;
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
