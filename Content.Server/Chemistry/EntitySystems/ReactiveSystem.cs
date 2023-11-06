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

        SubscribeLocalEvent<ReactiveComponent, SolutionSpilledEvent>(OnSolutionSpilled);
        SubscribeLocalEvent<ReactiveComponent, InventoryRelayedEvent<SolutionSpilledEvent>>(OnSolutionSpilled);
    }

    private void OnSolutionSpilled(EntityUid uid, ReactiveComponent component, ref SolutionSpilledEvent @event)
    {
        DoEntityReaction(uid, @event.Solution, ReactionMethod.Touch);
    }

    private void OnSolutionSpilled(EntityUid uid, ReactiveComponent component, InventoryRelayedEvent<SolutionSpilledEvent> @event)
    {
        DoEntityReaction(uid, @event.Args.Solution, ReactionMethod.Touch);
    }
}
