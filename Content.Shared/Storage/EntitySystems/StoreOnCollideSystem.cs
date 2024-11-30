using Content.Shared.Body.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Storage.EntitySystems;

internal sealed class StoreOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreOnCollideComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<StoreOnCollideComponent> ent, ref StartCollideEvent args)
    {
        // TODO: Add support to stop storage on collide after triggering once?
        // TODO: Add support to create storage dynamically

        if (ent.Owner == args.OtherEntity || Transform(args.OtherEntity).Anchored || _storage.IsOpen(ent.Owner) || (ent.Comp.BodyOnly && !HasComp<BodyComponent>(args.OtherEntity)))
            return;

        _storage.Insert(args.OtherEntity, ent.Owner);

        // TODO: Lock Support & figure out how to lock once
    }
}
