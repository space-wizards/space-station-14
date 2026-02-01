using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared.Body;

public sealed class HandOrganSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<HandOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<HandOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        // Container insertion/removal gets networked twice
        if (_timing.ApplyingState)
            return;

        _hands.AddHand(args.Target, ent.Comp.HandID, ent.Comp.Data);
    }

    private void OnGotRemoved(Entity<HandOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // Container insertion/removal gets networked twice
        if (_timing.ApplyingState)
            return;

        _hands.RemoveHand(args.Target, ent.Comp.HandID);
    }
}
