using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// This component spawns entities from a WeightedRandomEntityPrototype(aka entity table).
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
[Virtual]
public partial class GenericSpawnerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> EntityTable = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public List<EntProtoId> GameRules = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Chance { get; set; } = 1.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Offset { get; set; } = 0.2f;

    /// <summary>
    /// Spawner will pick an entity this amount of times. Must be between 1 and 100.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int Rolls { get; set; } = 1;

    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;

}
