using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Inventory;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class ReactiveSystem : SharedReactiveSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReactiveComponent, InventoryRelayedEvent<SolutionSpilledEvent>>(OnSolutionSpilled);
        SubscribeLocalEvent<ReactiveComponent, SolutionSpilledEvent>(OnSolutionSpilled);
    }

    private void OnSolutionSpilled(EntityUid uid, ReactiveComponent component, InventoryRelayedEvent<SolutionSpilledEvent> @event)
    {
        OnSolutionSpilled(uid, component, ref @event.Args);
    }

    private void OnSolutionSpilled(EntityUid uid, ReactiveComponent component, ref SolutionSpilledEvent @event)
    {
        DoEntityReaction(uid, @event.Solution.SplitSolution(@event.ToTakePerEntity), ReactionMethod.Touch);
    }
}