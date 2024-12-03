using Content.Shared.Body.Components;
using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Storage.EntitySystems;

internal sealed class StoreOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreOnCollideComponent, StartCollideEvent>(OnCollide);
        // TODO: Add support to stop colliding after throw, wands will need a WandComp
    }

    // We use Collide instead of Projectile to support different types of interactions
    private void OnCollide(Entity<StoreOnCollideComponent> ent, ref StartCollideEvent args)
    {
        // TODO: Add support to create storage dynamically
        // TODO: Add support to also include projectiles, will need to think of how to include it if bodyonly is checked

        var storageEnt = ent.Owner;
        var target = args.OtherEntity;
        var comp = ent.Comp;

        if (storageEnt == target || Transform(target).Anchored || _storage.IsOpen(storageEnt) || (comp.BodyOnly && !HasComp<BodyComponent>(target)))
            return;

        _storage.Insert(target, storageEnt);

        if (comp.LockOnCollide && !_lock.IsLocked(storageEnt))
            _lock.Lock(storageEnt, storageEnt);
    }
}
