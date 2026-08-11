using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class ActionSequenceSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, StartActionSequenceEvent>(OnStartSequence);
    }

    private void OnStartSequence(Entity<ActionSequenceComponent> ent, ref StartActionSequenceEvent args)
    {

    }
}

public abstract partial class XActionSequenceSystem<T> : EntitySystem where T : ActionSequence
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActionSequenceComponent, ActionSequenceEvent<T>>(OnSequence);
    }

    private void OnSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceEvent<T> args)
    {
        if (args.Sequence is not T sequence)
            return;

        if (ent.Comp.Sequences.IndexOf(sequence) != args.SequenceIndex)
            return;

        PerformSequence(ent, ref args);
    }

    private void OnStepped(Entity<ActionSequenceComponent> ent, ref ActionSequenceSteppedEvent args)
    {
        var ev = new ActionSequenceEvent<T>(ent, args.SequenceIndex);
    }

    public abstract void PerformSequence(Entity<ActionSequenceComponent> ent, ref ActionSequenceEvent<T> args);
}

public readonly struct StartActionSequenceEvent();

public record struct ActionSequenceSteppedEvent(EntityUid Action, int SequenceIndex, ActionSequence Sequence);

public record struct ActionSequenceEvent<T>(EntityUid Action, int SequenceIndex, T Sequence) where T : ActionSequence;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ActionSequence;

public partial class PopupSequence : ActionSequence
{
    [DataField]
    public string Text = "Test!";
}
