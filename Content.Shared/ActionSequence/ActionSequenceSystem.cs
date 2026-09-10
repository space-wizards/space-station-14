using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.ActionSequence.Steps;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

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
    public const string ActionStepLocationKey = "Location";

    #region Action Events
    [SubscribeLocalEvent]
    private void OnStartInstantSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceInstantEvent args)
    {
        if (ent.Comp.Awaiting != SequenceAwaiting.None)
            return;

        ent.Comp.EntityBlackboard = new Dictionary<string, EntityUid>();
        ent.Comp.CoordinateBlackboard = new Dictionary<string, NetCoordinates>();

        TryAddBlackboardData(ent, ActionStepActionKey, ent.Owner);
        TryAddBlackboardData(ent, ActionStepUserKey, args.Performer);

        args.Handled = true;

        StartSequence(ent);
    }

    [SubscribeLocalEvent]
    private void OnStartTargetSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceEntityTargetEvent args)
    {
        if (ent.Comp.Awaiting != SequenceAwaiting.None)
            return;

        ent.Comp.EntityBlackboard = new Dictionary<string, EntityUid>();
        ent.Comp.CoordinateBlackboard = new Dictionary<string, NetCoordinates>();
        TryAddBlackboardData(ent, ActionStepActionKey, ent.Owner);
        TryAddBlackboardData(ent, ActionStepUserKey, args.Performer);
        TryAddBlackboardData(ent, ActionStepTargetKey, args.Target);

        args.Handled = true;

        StartSequence(ent);

    }

    [SubscribeLocalEvent]
    private void OnStartWorldTargetSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceWorldTargetEvent args)
    {
        if (ent.Comp.Awaiting != SequenceAwaiting.None)
            return;

        ent.Comp.EntityBlackboard = new Dictionary<string, EntityUid>();
        ent.Comp.CoordinateBlackboard = new Dictionary<string, NetCoordinates>();
        TryAddBlackboardData(ent, ActionStepActionKey, ent.Owner);
        TryAddBlackboardData(ent, ActionStepUserKey, args.Performer);
        TryAddBlackboardData(ent, ActionStepLocationKey, args.Target);

        if (args.Entity != null)
        {
            TryAddBlackboardData(ent, ActionStepTargetKey, args.Entity);
        }

        args.Handled = true;

        StartSequence(ent);
    }
    #endregion

    [SubscribeLocalEvent]
    private void OnSequenceStep(Entity<ActionSequenceComponent> ent, ref ActionSequenceSteppedEvent args)
    {
        if (!ent.Comp.SequenceOngoing)
            return;

        // Don't step if we're waiting for something!
        if (ent.Comp.Awaiting != SequenceAwaiting.None)
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

    [SubscribeLocalEvent]
    private void OnSequenceDoAfter(Entity<ActionSequenceComponent> ent, ref ActionSequenceDoAfterEvent args)
    {
        if (ent.Comp.Awaiting != SequenceAwaiting.DoAfter || !ent.Comp.SequenceOngoing)
            return;

        if (args.Cancelled)
        {
            StopSequence(ent);
            return;
        }

        ent.Comp.Awaiting = SequenceAwaiting.None;
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.Awaiting));

        StepSequence(ent);
    }

    private void StartSequence(Entity<ActionSequenceComponent> ent)
    {
        ent.Comp.SequenceOngoing = true;
        ent.Comp.CurrentStep = 0;

        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.SequenceOngoing));
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.CurrentStep));

        StepSequence(ent);
    }

    private void StepSequence(Entity<ActionSequenceComponent> ent)
    {
        if (ent.Comp.Awaiting != SequenceAwaiting.None)
            return;

        var ev = new ActionSequenceSteppedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void StopSequence(Entity<ActionSequenceComponent> ent)
    {
        // The sequence is stopped, so we remove all relevant data and mark it as ready to start again.
        ent.Comp.SequenceOngoing = false;
        ent.Comp.Awaiting = SequenceAwaiting.None;
        ent.Comp.CurrentStep = 0;
        ent.Comp.EntityBlackboard = new Dictionary<string, EntityUid>();
        ent.Comp.CoordinateBlackboard = new Dictionary<string, NetCoordinates>();

        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.EntityBlackboard));
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.CoordinateBlackboard));
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.SequenceOngoing));
        DirtyField(ent, ent.Comp, nameof(ActionSequenceComponent.Awaiting));
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

    /// <summary>
    /// Gets the data of a relevant Type from the <see cref="ActionSequenceComponent.Blackboard"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionSequenceComponent"/> entity.</param>
    /// <param name="key">They key to retrieve the value of.</param>
    /// <param name="data">The data obtained from the blackboard.</param>
    /// <typeparam name="T">The type we are looking for.</typeparam>
    /// <returns>True if the data was found, otherwise False.</returns>
    public bool TryGetBlackboardData<T>(Entity<ActionSequenceComponent> action, string? key, [NotNullWhen(true)] out T? data)
    {
        data = default;

        if (key == null)
            return false;

        if (typeof(T) == typeof(EntityUid))
        {
            if (action.Comp.EntityBlackboard.TryGetValue(key, out var keyData) && keyData is T dataValue)
            {
                data = dataValue;
                return true;
            }

        }
        else if (typeof(T) == typeof(EntityCoordinates))
        {
            if (action.Comp.CoordinateBlackboard.TryGetValue(key, out var keyData) && keyData is T dataValue)
            {
                data = dataValue;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to add new data to the <see cref="ActionSequenceComponent.Blackboard"/> of the action entity.
    /// </summary>
    /// <param name="action">The <see cref="ActionSequenceComponent"/> entity.</param>
    /// <param name="key">They of the data to add/</param>
    /// <param name="data">The data to add.</param>
    /// <returns>True if the data was added, otherwise False.</returns>
    public bool TryAddBlackboardData(Entity<ActionSequenceComponent> action, string? key, object data)
    {
        if (key == null)
            return false;

        bool added = false;
        if (data is EntityUid dataUid)
        {
            added = action.Comp.EntityBlackboard.TryAdd(key, dataUid);
            DirtyField(action, action.Comp,  nameof(ActionSequenceComponent.EntityBlackboard));
        }

        if (data is NetCoordinates dataCoordinates)
        {
            added = action.Comp.CoordinateBlackboard.TryAdd(key, dataCoordinates);
            DirtyField(action, action.Comp,  nameof(ActionSequenceComponent.CoordinateBlackboard));
        }

        return added;
    }
}

/// <summary>
/// Basic abstract EntitySystem that handles doing the effects of an <see cref="ActionStep"/>
/// </summary>
/// <typeparam name="T">The type of <see cref="ActionStep"/> this system is for.</typeparam>
public abstract partial class ActionStepSystem<T> : EntitySystem where T : ActionStepBase<T>
{
    [Dependency] protected ActionSequenceSystem SequenceSystem = default!;

    [SubscribeLocalEvent]
    protected abstract void Step(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<T> args);
}

/// <summary>
/// Event to begin the action sequence when paired with <see cref="InstantActionEvent"/>
/// </summary>
[ByRefEvent]
public sealed partial class ActionSequenceInstantEvent : InstantActionEvent;

/// <summary>
/// Event to begin the action sequence when paired with <see cref="EntityTargetActionEvent"/>
/// </summary>
[ByRefEvent]
public sealed partial class ActionSequenceEntityTargetEvent : EntityTargetActionEvent;

/// <summary>
/// Event to begin the action sequence when paired with <see cref="WorldTargetActionEvent"/>
/// </summary>
[ByRefEvent]
public sealed partial class ActionSequenceWorldTargetEvent : WorldTargetActionEvent;

/// <summary>
/// DoAfter event raised by <see cref="DoAfterActionStep"/> when the doAfter is resolved.
/// Used to unlock the sequence.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ActionSequenceDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Event that handles the effects of action steps. Gets raised on the action itself and the effects get resolved by the relevant system.
/// </summary>
/// <param name="Step">The <see cref="ActionStep"/> this event is handling.</param>
/// <param name="Handled">Whether the step was handled. If not handled, the sequence is canceled.</param>
/// <param name="Await">Whether after this step is complete, the action should await or continue the sequence.</param>
/// <typeparam name="T">The <see cref="ActionStep"/> this event is handling.</typeparam>
[ByRefEvent]
public record struct ActionStepEvent<T>(T Step, bool Handled = false, SequenceAwaiting Await = SequenceAwaiting.None) where T : ActionStepBase<T>;

/// <summary>
/// Raised on the Action entity when the sequence has successfully stepped and can proceed to the next one.
/// </summary>
[ByRefEvent]
public record struct ActionSequenceSteppedEvent;
