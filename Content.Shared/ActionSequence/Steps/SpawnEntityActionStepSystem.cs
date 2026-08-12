using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SpawnEntityActionStepSystem : ActionStepSystem<SpawnEntityActionStep>
{
    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<SpawnEntityActionStep> args)
    {
        if (SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.LocationKey, out var user))
        {
            var spawned = PredictedSpawnNextToOrDrop(args.Step.Entity, user);

            SequenceSystem.TryAddBlackboardData(action, args.Step.OutSpawnedKey, spawned);
            args.Handled = true;
        }
        else if (SequenceSystem.TryGetBlackboardData<EntityCoordinates>(action, args.Step.LocationKey, out var coordinates))
        {
            var spawned = PredictedSpawnAtPosition(args.Step.Entity, coordinates);

            SequenceSystem.TryAddBlackboardData(action, args.Step.OutSpawnedKey, spawned);
            args.Handled = true;
        }
        else
        {
            Log.Error($"Action step {args.Step} in {ToPrettyString(action)} received invalid LocationKey.");
        }
    }
}

/// <summary>
/// Spawns an entity at the given LocationKey and adds the spawned entity to the blackboard as the OutSpawnedKey.
/// Can take either an EntityUid or EntityCoordinates.
/// </summary>
public sealed partial class SpawnEntityActionStep : ActionStepBase<SpawnEntityActionStep>
{
    /// <summary>
    /// The entity we want to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity = "MobMouse1";

    /// <summary>
    /// The location at which we want to spawn the entity.
    /// </summary>
    [DataField]
    public string LocationKey = ActionSequenceSystem.ActionStepUserKey;

    /// <summary>
    /// What key to add the spawned entity as, if any.
    /// </summary>
    [DataField]
    public string? OutSpawnedKey;
}
