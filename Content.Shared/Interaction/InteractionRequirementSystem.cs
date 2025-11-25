using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction;

public abstract partial class InteractionRequirementSystem<T, J>: InteractionRequirementSystem<T> where T: InteractionRequirementComponent, new() where J: TrackedInteractionRequirementComponent<T>, new()
{
    protected override void OnInteraction(Entity<T> ent, ref ConditionalInteractionEvent args)
    {
        base.OnInteraction(ent, ref args);

        var comp = EnsureComp<J>(args.Source);
        comp.Interactions.Add(ent.Owner);
    }

    protected override void OnInteractionEnd(Entity<T> ent, ref ConditionalInteractionEndEvent args)
    {
        base.OnInteractionEnd(ent, ref args);

        if (!TryComp<J>(args.Source, out var comp))
            return;

        comp.Interactions.Remove(ent.Owner);
    }

    protected void NotifyRequirementChangeSource(Entity<J?> ent, bool allow)
    {
        if (!TryComp(ent.Owner, out ent.Comp))
            return;

        if (!TryComp<J>(ent.Owner, out var comp))
            return;

        ToRemove.Clear();

        var ev = new InteractionConditionChangedEvent(ent.Owner, allow);
        ev.FailureSuffix = FailureSuffix;

        foreach (var uid in comp.Interactions)
        {
            ev.Cancelled = false;
            RaiseLocalEvent(uid, ref ev);

            if (ev.Cancelled)
                ToRemove.Add(uid);
        }

        foreach (var uid in ToRemove)
        {
            comp.Interactions.Remove(uid);
        }
    }
}

public abstract partial class InteractionRequirementSystem<T> : EntitySystem where T: InteractionRequirementComponent
{
    protected abstract string FailureSuffix { get; }
    protected virtual bool TrackInteractions => false;
    protected abstract bool Condition(Entity<T> ent, ref readonly ConditionalInteractionAttemptEvent args);
    protected List<EntityUid> ToRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ConditionalInteractionEvent>(OnInteraction);
        SubscribeLocalEvent<T, ConditionalInteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<T, ConditionalInteractionEndEvent>(OnInteractionEnd);
    }

    private void OnInteractionAttempt(Entity<T> ent, ref ConditionalInteractionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.RequiredFor != null && !ent.Comp.RequiredFor.Contains(args.InteractionType))
            return;

        if (Condition(ent, ref args))
            return;

        args.Cancelled = true;
        args.FailureSuffix = FailureSuffix;
    }

    protected virtual void OnInteraction(Entity<T> ent, ref ConditionalInteractionEvent args)
    {
        if (TrackInteractions)
            ent.Comp.Interactions.Add(args.Source);
    }

    protected virtual void OnInteractionEnd(Entity<T> ent, ref ConditionalInteractionEndEvent args)
    {
        if (TrackInteractions)
            ent.Comp.Interactions.Remove(args.Source);
    }

    protected void NotifyRequirementChange(Entity<T> ent, bool allow)
    {
        ToRemove.Clear();

        var ev = new InteractionConditionChangedEvent(ent.Owner, allow);
        ev.FailureSuffix = FailureSuffix;

        foreach (var uid in ent.Comp.Interactions)
        {
            ev.Cancelled = false;
            RaiseLocalEvent(uid, ref ev);

            if (ev.Cancelled)
                ToRemove.Add(uid);
        }

        foreach (var uid in ToRemove)
        {
            ent.Comp.Interactions.Remove(uid);
        }
    }

}

public sealed partial class InteractionRequirementSystem : EntitySystem
{
    /// <summary>
    /// </summary>
    /// <returns>
    /// true if interaction is possible,
    /// false if interaction is cannot be performed due to requirement
    /// </returns>
    public bool CanInteract(EntityUid source, EntityUid interacted, ProtoId<InteractionTypePrototype> interactionType, [NotNullWhen(false)] out string? kind)
    {
        kind = null;

        var evAttempt = new ConditionalInteractionAttemptEvent(source, interactionType);
        RaiseLocalEvent(interacted, ref evAttempt);

        if (evAttempt.Cancelled)
        {
            kind = evAttempt.FailureSuffix;
            return false;
        }

        var ev = new ConditionalInteractionEvent(source, interactionType);
        RaiseLocalEvent(interacted, ref ev);

        return true;
    }

    /// <summary>
    /// Explicitly ends interaction, clearing all information stored about it.
    /// </summary>
    public void InteractionEnd(EntityUid source, EntityUid interacted, ProtoId<InteractionTypePrototype> interactionType)
    {
        var evAttempt = new ConditionalInteractionEndEvent(source, interactionType);
        RaiseLocalEvent(interacted, ref evAttempt);
    }
}
