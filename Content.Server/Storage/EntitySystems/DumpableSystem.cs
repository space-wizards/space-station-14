using System.Threading;
using Content.Shared.Interaction;
using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.Placeable;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class DumpableSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly DisposalUnitSystem _disposalUnitSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DumpableComponent, AfterInteractEvent>(OnAfterInteract, after: new[]{ typeof(StorageSystem) });
            SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<AlternativeVerb>>(AddDumpVerb);
            SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<UtilityVerb>>(AddUtilityVerbs);
            SubscribeLocalEvent<DumpCompletedEvent>(OnDumpCompleted);
            SubscribeLocalEvent<DumpCancelledEvent>(OnDumpCancelled);
        }

        private void OnAfterInteract(EntityUid uid, DumpableComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ServerStorageComponent>(args.Used, out var storage))
                return;

            if (storage.StoredEntities == null || storage.StoredEntities.Count == 0 || storage.CancelToken != null)
                return;

            if (HasComp<DisposalUnitComponent>(args.Target) || HasComp<PlaceableSurfaceComponent>(args.Target))
            {
                StartDoAfter(uid, args.Target.Value, args.User, component, storage);
            }
        }

        private void AddDumpVerb(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.StoredEntities == null || storage.StoredEntities.Count == 0)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartDoAfter(uid, null, args.User, dumpable, storage, 0.6f);
                },
                Text = Loc.GetString("dump-verb-name"),
                IconTexture = "/Textures/Interface/VerbIcons/drop.svg.192dpi.png",
            };
            args.Verbs.Add(verb);
        }

        private void AddUtilityVerbs(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.StoredEntities == null || storage.StoredEntities.Count == 0)
                return;

            if (HasComp<DisposalUnitComponent>(args.Target))
            {
                UtilityVerb verb = new()
                {
                    Act = () =>
                    {
                        StartDoAfter(uid, args.Target, args.User, dumpable, storage);
                    },
                    Text = Loc.GetString("dump-disposal-verb-name", ("unit", args.Target)),
                    IconEntity = uid
                };
                args.Verbs.Add(verb);
            }

            if (HasComp<PlaceableSurfaceComponent>(args.Target))
            {
                UtilityVerb verb = new()
                {
                    Act = () =>
                    {
                        StartDoAfter(uid, args.Target, args.User, dumpable, storage);
                    },
                    Text = Loc.GetString("dump-placeable-verb-name", ("surface", args.Target)),
                    IconEntity = uid
                };
                args.Verbs.Add(verb);
            }
        }

        public void StartDoAfter(EntityUid storageUid, EntityUid? targetUid, EntityUid userUid, DumpableComponent dumpable, ServerStorageComponent storage, float multiplier = 1)
        {
            if (dumpable.CancelToken != null)
                return;

            if (storage.StoredEntities == null)
                return;

            float delay = storage.StoredEntities.Count * (float) dumpable.DelayPerItem.TotalSeconds * multiplier;

            dumpable.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(userUid, delay, dumpable.CancelToken.Token, target: targetUid)
            {
                BroadcastFinishedEvent = new DumpCompletedEvent(dumpable, userUid, targetUid, storage.StoredEntities),
                BroadcastCancelledEvent = new DumpCancelledEvent(dumpable.Owner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }


        private void OnDumpCompleted(DumpCompletedEvent args)
        {
            args.Component.CancelToken = null;

            Queue<EntityUid> dumpQueue = new();
            foreach (var entity in args.StoredEntities)
            {
                dumpQueue.Enqueue(entity);
            }

            if (TryComp<DisposalUnitComponent>(args.Target, out var disposal))
            {
                foreach (var entity in dumpQueue)
                {
                    _disposalUnitSystem.DoInsertDisposalUnit(args.Target.Value, entity, args.User);
                }
                return;
            }

            foreach (var entity in dumpQueue)
            {
                var transform = Transform(entity);
                transform.AttachParentToContainerOrGrid(EntityManager);
                transform.LocalPosition = transform.LocalPosition + _random.NextVector2Box() / 2;
                transform.LocalRotation = _random.NextAngle();
            }

            if (HasComp<PlaceableSurfaceComponent>(args.Target))
            {
                foreach (var entity in dumpQueue)
                {
                    Transform(entity).LocalPosition = Transform(args.Target.Value).LocalPosition + _random.NextVector2Box() / 4;
                }
            }
        }

        private void OnDumpCancelled(DumpCancelledEvent args)
        {
            if (TryComp<DumpableComponent>(args.Uid, out var dumpable))
                dumpable.CancelToken = null;
        }

        private sealed class DumpCancelledEvent : EntityEventArgs
        {
            public readonly EntityUid Uid;
            public DumpCancelledEvent(EntityUid uid)
            {
                Uid = uid;
            }
        }

        private sealed class DumpCompletedEvent : EntityEventArgs
        {
            public DumpableComponent Component { get; }
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public IReadOnlyList<EntityUid> StoredEntities { get; }

            public DumpCompletedEvent(DumpableComponent component, EntityUid user, EntityUid? target, IReadOnlyList<EntityUid> storedEntities)
            {
                Component = component;
                User = user;
                Target = target;
                StoredEntities = storedEntities;
            }
        }
    }
}
