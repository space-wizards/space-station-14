using Content.Shared.Actions;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class ActionSequenceSystem : EntitySystem, IActionStepRaiser
{
    public const string ActionStepUserKey = "Performer";
    public const string ActionStepTargetKey = "Target";
    public const string ActionStepActionKey = "Action";

    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceInstantEvent>(OnStartSequence);
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceSteppedEvent>(OnSequenceStep);
    }

    private void OnStartSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceInstantEvent args)
    {
        ent.Comp.Blackboard = new Dictionary<string, object>();
        ent.Comp.Blackboard.TryAdd(ActionStepActionKey, ent.Owner);
        ent.Comp.Blackboard.TryAdd(ActionStepUserKey, args.Performer);

        Log.Debug("Action sequence initiated");

        StartSequence(ent);
    }

    private void StartSequence(Entity<ActionSequenceComponent> ent)
    {
        ent.Comp.SequenceOngoing = true;
        ent.Comp.CurrentStep = 0;

        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.SequenceOngoing));

        Log.Debug("Action sequence started");

        StepSequence(ent);
    }

    private void StepSequence(Entity<ActionSequenceComponent> ent)
    {
        Log.Debug("Action sequence stepped");

        var ev = new ActionSequenceSteppedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnSequenceStep(Entity<ActionSequenceComponent> ent, ref ActionSequenceSteppedEvent args)
    {
        if (!ent.Comp.SequenceOngoing)
            return;

        ent.Comp.CurrentStep++;
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.CurrentStep));

        if (ent.Comp.CurrentStep > ent.Comp.Steps.Count)
        {
            StopSequence(ent);
            return;
        }

        Log.Debug("Action sequence event raised");

        ent.Comp.Steps[ent.Comp.CurrentStep-1].RaiseEvent(ent.Owner, this);
    }

    private void StopSequence(Entity<ActionSequenceComponent> ent)
    {
        ent.Comp.SequenceOngoing = false;
        ent.Comp.CurrentStep = 0;
        ent.Comp.Blackboard = new Dictionary<string, object>();

        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.SequenceOngoing));
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.CurrentStep));

        Log.Debug("Action sequence stopped");
    }

    public void RaiseEffectEvent<T>(EntityUid action, T effect) where T : ActionStepBase<T>
    {
        var effectEv = new ActionStepEvent<T>(effect);
        RaiseLocalEvent(action, ref effectEv);

        if (!effectEv.Handled || effectEv.Await)
            return;

        Log.Debug("Action sequence handled");

        var stepEv = new ActionSequenceSteppedEvent();
        RaiseLocalEvent(action, ref stepEv);
    }
}

/// <summary>
/// This is a basic abstract entity effect containing all the data an entity effect needs to affect entities with effects...
/// </summary>
/// <typeparam name="T">The Component that is required for the effect</typeparam>
public abstract partial class ActionStepSystem<T> : EntitySystem where T : ActionStepBase<T>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, ActionStepEvent<T>>(Effect);
    }

    protected abstract void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<T> args);
}

[ByRefEvent]
public record struct ActionStepEvent<T>(T Effect, bool Handled = false, bool Await = false) where T : ActionStepBase<T>;

[ByRefEvent]
public sealed partial class ActionSequenceInstantEvent : InstantActionEvent;

[ByRefEvent]
public record struct ActionSequenceSteppedEvent();
