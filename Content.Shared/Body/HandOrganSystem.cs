using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Body;

public sealed class HandOrganSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Body.HandOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<Body.HandOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<Body.HandOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        _hands.AddHand(args.Target, ent.Comp.HandID, ent.Comp.Data);
    }

    private void OnGotRemoved(Entity<Body.HandOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // prevent a recursive double-delete bug
        if (LifeStage(args.Target) >= EntityLifeStage.Terminating)
            return;

        _hands.RemoveHand(args.Target, ent.Comp.HandID);
    }
}
