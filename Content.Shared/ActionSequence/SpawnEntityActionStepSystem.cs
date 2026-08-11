using Content.Shared.EntityEffects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SpawnEntityActionStepSystem : ActionStepSystem<SpawnEntityActionStep>
{
    protected override void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<SpawnEntityActionStep> args)
    {
        Log.Debug("Spawn detected");
        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.LocationKey, out var userKey))
            return;

        if (userKey is EntityUid user)
        {
            var spawned = PredictedSpawnNextToOrDrop(args.Effect.Entity, user);

            if (args.Effect.OutSpawnedKey != null)
                entity.Comp.Blackboard.TryAdd(args.Effect.OutSpawnedKey, spawned);

            args.Handled = true;
        }
        else if (userKey is EntityCoordinates coordinates)
        {
            var spawned = PredictedSpawnAtPosition(args.Effect.Entity, coordinates);

            if (args.Effect.OutSpawnedKey != null)
                entity.Comp.Blackboard.TryAdd(args.Effect.OutSpawnedKey, spawned);

            args.Handled = true;
        }
        else
        {
            Log.Error($"Sequence {args.Effect} received invalid Key.");
        }
    }
}

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpawnEntityActionStep : ActionStepBase<SpawnEntityActionStep>
{
    /// <summary>
    ///     The gas we're creating
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity = "MobMouse1";

    [DataField(required: true)]
    public string LocationKey = ActionSequenceSystem.ActionStepUserKey;

    [DataField]
    public string? OutSpawnedKey;
}
