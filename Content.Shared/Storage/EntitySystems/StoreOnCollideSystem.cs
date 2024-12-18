using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

internal sealed class StoreOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<StoreOnCollideComponent, StorageAfterOpenEvent>(AfterOpen);
        // TODO: Add support to stop colliding after throw, wands will need a WandComp
    }

    // We use Collide instead of Projectile to support different types of interactions
    private void OnCollide(Entity<StoreOnCollideComponent> ent, ref StartCollideEvent args)
    {
        TryStoreTarget(ent, args.OtherEntity);

        TryLockStorage(ent);
    }

    private void AfterOpen(Entity<StoreOnCollideComponent> ent, ref StorageAfterOpenEvent args)
    {
        var comp = ent.Comp;

        if (comp is { DisableWhenFirstOpened: true, Disabled: false })
            comp.Disabled = true;
    }

    private void TryStoreTarget(Entity<StoreOnCollideComponent> ent, EntityUid target)
    {
        var storageEnt = ent.Owner;
        var comp = ent.Comp;

        if (_netMan.IsClient || _gameTiming.ApplyingState)
            return;

        if (ent.Comp.Disabled || storageEnt == target || Transform(target).Anchored || _storage.IsOpen(storageEnt) || _whitelist.IsWhitelistFail(comp.Whitelist, target))
            return;

        _storage.Insert(target, storageEnt);

    }

    private void TryLockStorage(Entity<StoreOnCollideComponent> ent)
    {
        var storageEnt = ent.Owner;
        var comp = ent.Comp;

        if (_netMan.IsClient || _gameTiming.ApplyingState)
            return;

        if (ent.Comp.Disabled)
            return;

        if (comp.LockOnCollide && !_lock.IsLocked(storageEnt))
            _lock.Lock(storageEnt, storageEnt);
    }
}
