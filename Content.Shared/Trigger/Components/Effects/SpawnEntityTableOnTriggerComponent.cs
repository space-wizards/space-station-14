using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Spawns an entity table at this entity when triggered.
/// If TargetUser is true it will be spawned at their location.
/// </summary>
/// <seealso cref="SpawnOnTriggerComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpawnEntityTableOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The table to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table;

    /// <summary>
    /// Use MapCoordinates for spawning?
    /// Set to true if you don't want the new entity parented to the spawner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseMapCoords;

    /// <summary>
    /// Whether to use predicted spawning.
    /// </summary>
    /// <remarks>Randomization in EntityTables is not currently predicted! Use with caution.</remarks>
    [DataField, AutoNetworkedField]
    public bool Predicted;
}
