using Content.Shared.EntityEffects;
using Content.Shared.Mind;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class TrasnferMindActionStepSystem : ActionStepSystem<TransferMindActionStep>
{
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<TransferMindActionStep> args)
    {
        Log.Debug("Mind detected.");
        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.TargetKey, out var targetKey) || targetKey is not EntityUid target)
            return;

        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.MindKey, out var mindKey) || mindKey is not EntityUid mind)
            return;

        if (!HasComp<MindComponent>(mind))
        {
            Log.Error($"Entity {mind} does not have MindComponent!");
            return;
        }

        _mind.TransferTo(mind, target);

        args.Handled = true;
    }
}

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TransferMindActionStep : ActionStepBase<TransferMindActionStep>
{
    [DataField]
    public string MindKey = "Mind";
}
