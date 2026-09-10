using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// A spawner that rolls to spawn from of a list of entities on <see cref="MapInitEvent"/> and <see cref="GameRuleStartedEvent"/>.
/// </summary>
/// <remarks>
/// For non-trivial lists of prototypes, consider using <see cref="EntityTableSpawnerComponent"/> and an <see cref="EntityTableSelector"/> instead.
/// </remarks>
[RegisterComponent, EntityCategory("Spawner")]
[Virtual]
public partial class ConditionalSpawnerComponent : Component
{
    /// <summary>
    /// A list of entities, one of which can spawn on a spawn roll when calling <see cref="ConditionalSpawnerSystem.Spawn"/>.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes { get; set; } = new();

    /// <summary>
    /// A list of game rules - starting any of them causes a spawn roll.
    /// </summary>
    /// <remarks>
    /// Currently unused, should be marked obsolete if not removed outright.
    /// </remarks>
    [DataField]
    public List<EntProtoId> GameRules = new();

    /// <summary>
    /// Chance of spawning an entity on each spawn roll.
    /// </summary>
    [DataField]
    public float Chance { get; set; } = 1.0f;
}
