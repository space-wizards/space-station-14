using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Storage.EntitySystems;

internal sealed class StoreOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreOnCollideComponent, StartCollideEvent>(OnCollide);
        // TODO: Add support to stop colliding after throw, wands will need a WandComp
    }

    // We use Collide instead of Projectile to support different types of interactions
    private void OnCollide(Entity<StoreOnCollideComponent> ent, ref StartCollideEvent args)
    {
        TryStoreTarget(ent, args.OtherEntity);
    }

    private void TryStoreTarget(Entity<StoreOnCollideComponent> ent, EntityUid target)
    {
        var storageEnt = ent.Owner;
        var comp = ent.Comp;

        if (storageEnt == target || Transform(target).Anchored || _storage.IsOpen(storageEnt) || HasComp<StoreOnCollideComponent>(target) || _whitelist.IsWhitelistFail(comp.Whitelist, target))
            return;

        _storage.Insert(target, storageEnt);

        TryLockStorage(ent);
    }

    private void TryLockStorage(Entity<StoreOnCollideComponent> ent)
    {
        var storageEnt = ent.Owner;
        var comp = ent.Comp;

        if (comp.LockOnCollide && !_lock.IsLocked(storageEnt))
            _lock.Lock(storageEnt, storageEnt);
    }
}
