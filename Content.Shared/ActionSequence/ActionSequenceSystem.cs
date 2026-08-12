using Content.Shared.Actions;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handled the general behavior of action sequences.
/// Action sequences run specified behavior in a sequence one after another, communicating via a blackboard.
/// This allows to make actions follow a pattern using a generic system, such as DoAfter -> Spawn Entity -> Play Sound.
/// </summary>
public sealed partial class ActionSequenceSystem : EntitySystem, IActionStepRaiser
{
    public const string ActionStepUserKey = "Performer";
    public const string ActionStepTargetKey = "Target";
    public const string ActionStepActionKey = "Action";

    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceInstantEvent>(OnStartInstantSequence);
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceSteppedEvent>(OnSequenceStep);
    }

    private void OnStartInstantSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceInstantEvent args)
    {
        ent.Comp.Blackboard = new Dictionary<string, object>();
        ent.Comp.Blackboard.TryAdd(ActionStepActionKey, ent.Owner);
        ent.Comp.Blackboard.TryAdd(ActionStepUserKey, args.Performer);

        StartSequence(ent);
    }

    private void StartSequence(Entity<ActionSequenceComponent> ent)
    {
        ent.Comp.SequenceOngoing = true;
        ent.Comp.CurrentStep = 0;

        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.SequenceOngoing));

        StepSequence(ent);
    }

    private void StepSequence(Entity<ActionSequenceComponent> ent)
    {
        var ev = new ActionSequenceSteppedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnSequenceStep(Entity<ActionSequenceComponent> ent, ref ActionSequenceSteppedEvent args)
    {
        if (!ent.Comp.SequenceOngoing)
            return;

        ent.Comp.CurrentStep++;
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.CurrentStep));

        // If we're at the end of the sequence, stop it.
        if (ent.Comp.CurrentStep > ent.Comp.Steps.Count)
        {
            StopSequence(ent);
            return;
        }

        ent.Comp.Steps[ent.Comp.CurrentStep-1].RaiseEvent(ent, this);
    }

    private void StopSequence(Entity<ActionSequenceComponent> ent)
    {
        // The sequence is stopped, so we remove all relevant data and mark it as ready to start again.
        ent.Comp.SequenceOngoing = false;
        ent.Comp.CurrentStep = 0;
        ent.Comp.Blackboard = new Dictionary<string, object>();

        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.SequenceOngoing));
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.CurrentStep));
    }

    public void RaiseStepEvent<T>(Entity<ActionSequenceComponent> action, T effect) where T : ActionStepBase<T>
    {
        var effectEv = new ActionStepEvent<T>(effect);
        RaiseLocalEvent(action, ref effectEv);

        action.Comp.Awaiting = effectEv.Await;
        DirtyField(action, action.Comp, nameof(ActionSequenceComponent.Awaiting));

        // We only want to take the next step if the event was actually handled.
        // And if we aren't waiting for something to continue it for us (like a doAfter)
        if (!effectEv.Handled && effectEv.Await != SequenceAwaiting.None)
            return;

        StepSequence(action);
    }
}

/// <summary>
/// Basic abstract EntitySystem that handles doing the effects of an <see cref="ActionStep"/>
/// </summary>
/// <typeparam name="T">The type of <see cref="ActionStep"/> this system is for.</typeparam>
public abstract partial class ActionStepSystem<T> : EntitySystem where T : ActionStepBase<T>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, ActionStepEvent<T>>(Effect);
    }

    protected abstract void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<T> args);
}

/// <summary>
/// Event to begin the action sequence when paired with <see cref="InstantActionEvent"/>
/// </summary>
[ByRefEvent]
public sealed partial class ActionSequenceInstantEvent : InstantActionEvent;

/// <summary>
/// Event that handles the effects of action steps. Gets raised on the action itself and the effects get resolved by the relevant system.
/// </summary>
/// <param name="Effect">The <see cref="ActionStep"/> this event is handling.</param>
/// <param name="Handled">Whether the step was handled. If not handled, the sequence is canceled.</param>
/// <param name="Await">Whether after this step is complete, the action should await or continue the sequence.</param>
/// <typeparam name="T">The <see cref="ActionStep"/> this event is handling.</typeparam>
[ByRefEvent]
public record struct ActionStepEvent<T>(T Effect, bool Handled = false, SequenceAwaiting Await = SequenceAwaiting.None) where T : ActionStepBase<T>;

/// <summary>
/// Raised on the Action entity when the sequence has successfully stepped and can proceed to the next one.
/// </summary>
[ByRefEvent]
public record struct ActionSequenceSteppedEvent;
