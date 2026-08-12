namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="AwaitReactivationActionStep"/>.
/// </summary>
public sealed partial class AwaitReactivationActionStepSystem : ActionStepSystem<AwaitReactivationActionStep>
{
    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<AwaitReactivationActionStep> args)
    {
        action.Comp.AwaitingKey = args.Step.OutKey;

        DirtyField(action, action.Comp, nameof(ActionSequenceComponent.Awaiting));
        DirtyField(action, action.Comp, nameof(ActionSequenceComponent.AwaitingKey));

        args.Handled = true;
        args.Await = SequenceAwaiting.Reactivation;
    }
}

/// <summary>
/// Stops further sequence steps until the action is reused.
/// Reaching this step means the action was handled.
/// </summary>
public sealed partial class AwaitReactivationActionStep : ActionStepBase<AwaitReactivationActionStep>
{
    /// <summary>
    /// Optional key to pass to the action sequence. Takes value depending on the type of action being reactivated.
    /// This will be the Target EntityUid when paired with <see cref="ActionSequenceEntityTargetEvent"/>
    /// Or EntityCoordinates when paired with <see cref="ActionSequenceEntityTargetEvent"/>
    /// </summary>
    [DataField]
    public string? OutKey;
}
