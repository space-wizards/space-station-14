using Content.Shared.EntityEffects;
using Content.Shared.Mind;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class GetMindActionStepSystem : ActionStepSystem<GetMindActionStep>
{
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<GetMindActionStep> args)
    {
        Log.Debug("Mind detected.");
        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.TargetKey, out var targetKey) || targetKey is not EntityUid target)
            return;

        _mind.TryGetMind(target, out var mindId, out _);
        entity.Comp.Blackboard.TryAdd(args.Effect.OutMindKey, mindId);

        args.Handled = true;
    }
}

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class GetMindActionStep : ActionStepBase<GetMindActionStep>
{
    [DataField]
    public string OutMindKey = "Mind";
}
