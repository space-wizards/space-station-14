using Content.Shared.Mind;

namespace Content.Shared.ActionSequence.Steps;

/// System handling <see cref="TransferMindActionStep"/>.
public sealed partial class TrasnferMindActionStepSystem : ActionStepSystem<TransferMindActionStep>
{
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<TransferMindActionStep> args)
    {
        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target))
            return;

        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.MindKey, out var mind))
            return;

        if (!HasComp<MindComponent>(mind))
        {
            Log.Error($"Entity {ToPrettyString(mind)} given to TransferMindActionStep is not a mind!");
            return;
        }

        if (args.Step.MakeSentient)
            _mind.MakeSentient(target);

        _mind.TransferTo(mind, target);

        args.Handled = true;
    }
}

/// <summary>
/// Transfers the MindKey to the TargetKey entity.
/// </summary>
public sealed partial class TransferMindActionStep : ActionStepBase<TransferMindActionStep>
{
    /// <summary>
    /// The key used to get the mind.
    /// </summary>
    [DataField]
    public string MindKey = "Mind";

    /// <summary>
    /// Whether to make the target sentient when mind is transferred.
    /// </summary>
    [DataField]
    public bool MakeSentient = true;
}
