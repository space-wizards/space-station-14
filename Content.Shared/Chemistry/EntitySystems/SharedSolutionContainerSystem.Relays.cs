using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.EntitySystems;

#region Events

/// <summary>
/// An event raised when more reagents are added to a (managed) solution than it can hold.
/// </summary>
[ByRefEvent]
public record struct SolutionContainerOverflowEvent(EntityUid SolutionEnt, Solution SolutionHolder, Solution Overflow)
{
    /// <summary>The entity which contains the solution that has overflowed.</summary>
    public readonly EntityUid SolutionEnt = SolutionEnt;
    /// <summary>The solution that has overflowed.</summary>
    public readonly Solution SolutionHolder = SolutionHolder;
    /// <summary>The reagents that have overflowed the solution.</summary>
    public readonly Solution Overflow = Overflow;
    /// <summary>The volume by which the solution has overflowed.</summary>
    public readonly FixedPoint2 OverflowVol = Overflow.Volume;
    /// <summary>Whether some subscriber has taken care of the effects of the overflow.</summary>
    public bool Handled = false;
}

/// <summary>
/// Ref event used to relay events raised on solution entities to their containers.
/// </summary>
/// <typeparam name="TEvent"></typeparam>
/// <param name="Event">The event that is being relayed.</param>
/// <param name="Solution">The container entity that the event is being relayed to.</param>
[ByRefEvent]
public record struct SolutionRelayEvent<TEvent>(TEvent Event, Entity<SolutionComponent> Solution)
{
    public readonly Entity<SolutionComponent> Solution = Solution;
    public TEvent Event = Event;
}

/// <summary>
/// Ref event used to relay events raised on solution containers to their contained solutions.
/// </summary>
/// <typeparam name="TEvent"></typeparam>
/// <param name="Event">The event that is being relayed.</param>
/// <param name="SolutionEnt">The solution entity that the event is being relayed to.</param>
/// <param name="Name">The name of the solution entity that the event is being relayed to.</param>
[ByRefEvent]
public record struct SolutionContainerRelayEvent<TEvent>(TEvent Event, Entity<SolutionComponent> SolutionEnt, string Name)
{
    public readonly Entity<SolutionComponent> SolutionEnt = SolutionEnt;
    public readonly string Name = Name;
    public TEvent Event = Event;
}

#endregion Events

public abstract partial class SharedSolutionContainerSystem
{
    protected void InitializeRelays()
    {
        SubscribeLocalEvent<ContainedSolutionComponent, SolutionOverflowEvent>(OnSolutionOverflow);
        SubscribeLocalEvent<ContainedSolutionComponent, ReactionAttemptEvent>(RelaySolutionRefEvent);
    }

    #region Event Handlers

    protected virtual void OnSolutionOverflow(Entity<ContainedSolutionComponent> entity, ref SolutionOverflowEvent args)
    {
        var solution = args.Solution.Comp.Solution;
        var overflow = solution.SplitSolution(args.Overflow);
        var relayEv = new SolutionContainerOverflowEvent(entity.Owner, solution, overflow)
        {
            Handled = args.Handled,
        };

        RaiseLocalEvent(entity.Comp.Container, ref relayEv);
        args.Handled = relayEv.Handled;
    }

    #region Relay Event Handlers

    private void RelaySolutionValEvent<TEvent>(EntityUid uid, ContainedSolutionComponent comp, TEvent @event)
    {
        var solution = Comp<SolutionComponent>(uid);
        var relayEvent = new SolutionRelayEvent<TEvent>(@event, (uid, solution));
        RaiseLocalEvent(comp.Container, ref relayEvent);
    }

    private void RelaySolutionRefEvent<TEvent>(Entity<ContainedSolutionComponent> entity, ref TEvent @event)
    {
        var solution = Comp<SolutionComponent>(entity);
        var relayEvent = new SolutionRelayEvent<TEvent>(@event, (entity, solution));
        RaiseLocalEvent(entity.Comp.Container, ref relayEvent);
        @event = relayEvent.Event;
    }

    private void RelaySolutionContainerEvent<TEvent>(EntityUid uid, SolutionManagerComponent comp, TEvent @event)
    {
        foreach (var (name, soln) in EnumerateSolutions((uid, comp)))
        {
            var relayEvent = new SolutionContainerRelayEvent<TEvent>(@event, soln, name!);
            RaiseLocalEvent(soln, ref relayEvent);
        }
    }

    private void RelaySolutionContainerEvent<TEvent>(Entity<SolutionManagerComponent> entity, ref TEvent @event)
    {
        foreach (var (name, soln) in EnumerateSolutions((entity.Owner, entity.Comp)))
        {
            var relayEvent = new SolutionContainerRelayEvent<TEvent>(@event, soln, name!);
            RaiseLocalEvent(soln, ref relayEvent);
            @event = relayEvent.Event;
        }
    }

    #endregion Relay Event Handlers

    #endregion Event Handlers
}
