using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Storage.Components;

/// <summary>
/// This component spawns a random entity from a list when its container is opened.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnOnOpenComponent : Component
{
    /// <summary>
    /// List of entity prototypes to randomly choose from when spawning.
    /// </summary>
    [DataField("prototypes", required: true)]
    public List<string> Prototypes = new();

    /// <summary>
    /// Chance to spawn an entity. Default is 100%.
    /// </summary>
    [DataField("chance")]
    public float Chance = 1.0f;

    /// <summary>
    /// RNG seed for consistent random selection.
    /// </summary>
    [DataField("rng")]
    public string? RngId;
    
    /// <summary>
    /// Whether this component has already spawned an entity.
    /// </summary>
    [DataField("hasSpawned")]
    public bool HasSpawned = false;
}
