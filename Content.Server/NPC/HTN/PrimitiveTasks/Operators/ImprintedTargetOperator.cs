using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Selects the leader, or the nearest hunt target followed by a point order.
/// </summary>
public sealed partial class ImprintedTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    [DataField]
    public bool Leader;

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entities.TryGetComponent<NPCImprintedComponent>(owner, out var imprint))
            return Task.FromResult<(bool, Dictionary<string, object>?)>((false, null));

        EntityUid? selected = null;
        if (Leader)
        {
            if (imprint.Leader is { } leader && !_entities.Deleted(leader))
                selected = leader;
        }
        else
        {
            var range = blackboard.GetValueOrDefault<float>(blackboard.GetVisionRadiusKey(_entities), _entities);
            var nearest = float.PositiveInfinity;
            var coordinates = new EntityCoordinates(owner, Vector2.Zero);
            foreach (var target in imprint.Target)
            {
                if (!CanTarget(target, imprint))
                    continue;

                if (!coordinates.TryDistance(_entities, new EntityCoordinates(target, Vector2.Zero), out var distance) ||
                    distance > range || distance >= nearest)
                    continue;

                if (!_interaction.InRangeUnobstructed(owner, target, range, CollisionGroup.Opaque))
                    continue;

                nearest = distance;
                selected = target;
            }

            if (selected == null &&
                blackboard.TryGetValue<EntityUid>(NPCBlackboard.CurrentOrderedTarget, out var ordered, _entities) &&
                CanTarget(ordered, imprint))
            {
                selected = ordered;
            }
        }

        if (selected is not { } entity)
            return Task.FromResult<(bool, Dictionary<string, object>?)>((false, null));

        var effects = new Dictionary<string, object>();
        if (Leader)
        {
            effects[NPCBlackboard.FollowTarget] = new EntityCoordinates(entity, Vector2.Zero);
        }
        else
        {
            effects["Target"] = entity;
            effects["TargetCoordinates"] = new EntityCoordinates(entity, Vector2.Zero);
        }

        return Task.FromResult<(bool, Dictionary<string, object>?)>((true, effects));
    }

    private bool CanTarget(EntityUid target, NPCImprintedComponent imprint)
    {
        if (_entities.Deleted(target) || imprint.Friendly.Contains(target))
            return false;

        return !_entities.TryGetComponent<MobStateComponent>(target, out var state) ||
               state.CurrentState != MobState.Dead;
    }
}
