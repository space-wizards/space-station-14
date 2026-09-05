using Content.Shared.Mind;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="GetMindActionStep"/>.
/// </summary>
public sealed partial class GetMindActionStepSystem : ActionStepSystem<GetMindActionStep>
{
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<GetMindActionStep> args)
    {
        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target))
            return;

        if (_mind.TryGetMind(target, out var mindId, out _))
        {
            SequenceSystem.TryAddBlackboardData(action, args.Step.OutMindKey, mindId);
            args.Handled = true;
        }
    }
}

/// <summary>
/// Gets the mind EntityUid of the TargetKey and adds it to the blackboard as the OutMindKey.
/// </summary>
public sealed partial class GetMindActionStep : ActionStepBase<GetMindActionStep>
{
    [DataField]
    public string OutMindKey = "Mind";
}
