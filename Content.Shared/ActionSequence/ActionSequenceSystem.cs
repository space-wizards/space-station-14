using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class ActionSequenceSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceInstantEvent>(OnStartSequence);
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceSteppedEvent>(OnSequenceStep);
    }

    private void OnStartSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceInstantEvent args)
    {
        var blackboard = new Dictionary<string, object>();
        blackboard.Add("Action", ent.Owner);
        blackboard.Add("Performer", args.Performer);

        StartSequence(ent, blackboard);
    }

    private void StartSequence(Entity<ActionSequenceComponent> ent, Dictionary<string, object> blackboard)
    {
        ent.Comp.Blackboard = blackboard;
        ent.Comp.SequenceOngoing = true;
        ent.Comp.CurrentStep = 0;

        StepSequence(ent, blackboard);
    }

    private void StepSequence(Entity<ActionSequenceComponent> ent, Dictionary<string, object> blackboard)
    {
        ent.Comp.CurrentStep++;
        var ev = new ActionSequenceSteppedEvent(ent.Comp.CurrentStep);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnSequenceStep(Entity<ActionSequenceComponent> ent, ref ActionSequenceSteppedEvent args)
    {
        if (!ent.Comp.SequenceOngoing)
            return;

        if (ent.Comp.CurrentStep > ent.Comp.Sequences.Count)
            return;

        var success = ent.Comp.Sequences[ent.Comp.CurrentStep-1].DoSequence(ent, ref ent.Comp.Blackboard, EntityManager);

        if (success)
            StepSequence(ent, ent.Comp.Blackboard);
    }
}

[ByRefEvent]
public sealed partial class ActionSequenceInstantEvent : InstantActionEvent;

[ByRefEvent]
public record struct ActionSequenceSteppedEvent(int SequenceIndex);

[ImplicitDataDefinitionForInheritors]
public abstract partial class ActionSequence
{
    [DataField]
    public string UserKey = "Performer";

    [DataField]
    public bool StopOnFail = true;

    public abstract bool DoSequence(EntityUid action, ref Dictionary<string, object> blackboard, EntityManager entMan);
}

public sealed partial class PopupSequence : ActionSequence
{
    [DataField]
    public string Text = "Test!";

    public override bool DoSequence(EntityUid action, ref Dictionary<string, object> blackboard, EntityManager entMan)
    {
        if (!blackboard.TryGetValue(UserKey, out var value))
            return false;

        if (value is not EntityUid viewer)
            return false;

        var popup = entMan.System<SharedPopupSystem>();
        popup.PopupEntity(Text, viewer, viewer);

        blackboard.Add("NewValue", "Hiiii!!");

        return true;
    }
}
