using Content.Server.Storage.Components;
using Content.Shared.Jittering;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Lock;
using Robust.Shared.Containers;

namespace Content.Server.Containers
{
    public sealed class ShredderSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedEntityStorageSystem _entityStorageSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly LockSystem _lockSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ShredderComponent>();
            while (query.MoveNext(out var uid, out var shredder))
            {
                if (shredder.ShreddingTimeLeft > 0)
                {
                    shredder.ShreddingTimeLeft -= frameTime;

                    if (shredder.ShreddingTimeLeft > 0)
                        return;

                    var manager = EnsureComp<ContainerManagerComponent>(uid);
                    var container = _containerSystem.EnsureContainer<BaseContainer>(uid, shredder.Container, manager);
                    for (var i = container.ContainedEntities.Count - 1; i >= 0; i--)
                    {
                        if (!shredder.Whitelist.IsValid(container.ContainedEntities[i], EntityManager))
                            continue;

                        var ev = new DoneBeingShreddedEvent(uid);
                        RaiseLocalEvent(container.ContainedEntities[i], ev);
                    }

                    if (shredder.OpenOnDone && TryComp(uid, out EntityStorageComponent? storage) && !storage.Open)
                    {
                        if (TryComp<LockComponent>(uid, out var lockComp))
                            _lockSystem.Unlock(uid, null, lockComp);
                        _entityStorageSystem.OpenStorage(uid, storage);
                    }

                    RemComp<JitteringComponent>(uid);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShredderComponent, EntInsertedIntoContainerMessage>(OnInsert);
        }
        private void OnInsert(EntityUid uid, ShredderComponent comp, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != comp.Container)
                return;

            if (!comp.Whitelist.IsValid(args.Entity, EntityManager))
                return;

            var shredEvent = new StartBeingShreddedEvent(args.Entity);
            RaiseLocalEvent(args.Entity, shredEvent);
            if (shredEvent.Handled)
            {
                comp.ShreddingTimeLeft = comp.ShreddingTime;
                _audio.PlayPvs(comp.ShreddingSound, uid);
                if (comp.DoShake)
                    _jitteringSystem.AddJitter(uid, 10f, 100f);

                if (comp.LockWhileShredding)
                {
                    var lockComp = EnsureComp<LockComponent>(uid);
                    _lockSystem.Lock(uid, null, lockComp);
                }
            }
        }
    }
}
