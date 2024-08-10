using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Abilities.Goliath.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGoliathTentacleSystem))]
public sealed partial class GoliathTentacleUserComponent : Component
{
    /// <summary>
    /// The ID of the entity that is spawned from <see cref="TentacleAction"/>
    /// </summary>
    [DataField]
    public EntProtoId TentacleId = "EffectGoliathTentacleSpawn";

    [DataField]
    public EntProtoId<EntityWorldTargetActionComponent> TentacleActionId = "ActionGoliathTentacle";

    [DataField]
    public EntityUid? TentacleAction;

    /// <summary>
    /// Directions determining where the tentacles will spawn.
    /// </summary>
    [DataField]
    public List<Direction> OffsetDirections = new()
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    /// <summary>
    /// How many tentacles will spawn beyond the original one at the target location?
    /// </summary>
    [DataField]
    public int ExtraSpawns = 3;
}

public sealed partial class GoliathSummonTentacleAction : EntityWorldTargetActionEvent;
