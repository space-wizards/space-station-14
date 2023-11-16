using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposableSystem : EntitySystem
    {
        [Dependency] private readonly DisposalUnitSystem _disposalUnit = default!;
        [Dependency] private readonly DisposalTubeSystem _disposalTube = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly MapSystem _map = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalHolderComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnComponentStartup(EntityUid uid, DisposalHolderComponent holder, ComponentStartup args)
        {
            holder.Container = _container.EnsureContainer<Container>(uid, nameof(DisposalHolderComponent));
        }

        public bool TryInsert(EntityUid uid, EntityUid toInsert, DisposalHolderComponent? holder = null)
        {
            if (!Resolve(uid, ref holder))
                return false;
            if (!CanInsert(uid, toInsert, holder))
                return false;

            if (!holder.Container.Insert(toInsert, EntityManager))
                return false;

            if (TryComp<PhysicsComponent>(toInsert, out var physBody))
                _physics.SetCanCollide(toInsert, false, body: physBody);

            return true;
        }

        private bool CanInsert(EntityUid uid, EntityUid toInsert, DisposalHolderComponent? holder = null)
        {
            if (!Resolve(uid, ref holder))
                return false;

            if (!_container.CanInsert(toInsert, holder.Container))
            {
                return false;
            }

            return HasComp<ItemComponent>(toInsert) ||
                   HasComp<BodyComponent>(toInsert);
        }

        public void ExitDisposals(EntityUid uid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null)
        {
            if (Terminating(uid))
                return;

            if (!Resolve(uid, ref holder, ref holderTransform))
                return;
            if (holder.IsExitingDisposals)
            {
                Log.Error("Tried exiting disposals twice. This should never happen.");
                return;
            }
            holder.IsExitingDisposals = true;

            // Check for a disposal unit to throw them into and then eject them from it.
            // *This ejection also makes the target not collide with the unit.*
            // *This is on purpose.*

            EntityUid? disposalId = null;
            DisposalUnitComponent? duc = null;
            var gridUid = holderTransform.GridUid;
            if (TryComp<MapGridComponent>(gridUid, out var grid))
            {
                var ducQuery = GetEntityQuery<DisposalUnitComponent>();
                foreach (var contentUid in _map.GetLocal(gridUid.Value, grid, holderTransform.Coordinates))
                {
                    if (ducQuery.TryGetComponent(contentUid, out duc))
                    {
                        disposalId = contentUid;
                        break;
                    }
                }
            }

            var physQuery = GetEntityQuery<PhysicsComponent>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var transformQuery = GetEntityQuery<TransformComponent>();
            foreach (var entity in holder.Container.ContainedEntities.ToArray())
            {
                RemComp<BeingDisposedComponent>(entity);

                var meta = metaQuery.GetComponent(entity);
                holder.Container.Remove(entity, EntityManager, meta: meta, reparent: false, force: true);

                var xform = transformQuery.GetComponent(entity);
                if (xform.ParentUid != uid)
                    continue;

                if (duc != null)
                    duc.Container.Insert(entity, EntityManager, xform, meta: meta);
                else
                    _transform.AttachToGridOrMap(entity, xform);

                if (physQuery.TryGetComponent(entity, out var physics))
                {
                    _physics.WakeBody(entity, body: physics);
                }
            }

            if (disposalId != null && duc != null)
            {
                _disposalUnit.TryEjectContents(disposalId.Value, duc);
            }

            if (_atmosphere.GetContainingMixture(uid, false, true) is { } environment)
            {
                _atmosphere.Merge(environment, holder.Air);
                holder.Air.Clear();
            }

            EntityManager.DeleteEntity(uid);
        }

        // Note: This function will cause an ExitDisposals on any failure that does not make an ExitDisposals impossible.
        public bool EnterTube(EntityUid holderUid, EntityUid toUid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null, DisposalTubeComponent? to = null, TransformComponent? toTransform = null)
        {
            if (!Resolve(holderUid, ref holder, ref holderTransform))
                return false;
            if (holder.IsExitingDisposals)
            {
                Log.Error("Tried entering tube after exiting disposals. This should never happen.");
                return false;
            }
            if (!Resolve(toUid, ref to, ref toTransform))
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            foreach (var ent in holder.Container.ContainedEntities)
            {
                var comp = EnsureComp<BeingDisposedComponent>(ent);
                comp.Holder = holderUid;
            }

            // Insert into next tube
            if (!to.Contents.Insert(holderUid))
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            if (holder.CurrentTube != null)
            {
                holder.PreviousTube = holder.CurrentTube;
                holder.PreviousDirection = holder.CurrentDirection;
            }
            holder.CurrentTube = toUid;
            var ev = new GetDisposalsNextDirectionEvent(holder);
            RaiseLocalEvent(toUid, ref ev);
            holder.CurrentDirection = ev.Next;
            holder.StartingTime = 0.1f;
            holder.TimeLeft = 0.1f;
            // Logger.InfoS("c.s.disposal.holder", $"Disposals dir {holder.CurrentDirection}");

            // Invalid direction = exit now!
            if (holder.CurrentDirection == Direction.Invalid)
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            // damage entities on turns and play sound
            if (holder.CurrentDirection != holder.PreviousDirection)
            {
                foreach (var ent in holder.Container.ContainedEntities)
                {
                    _damageable.TryChangeDamage(ent, to.DamageOnTurn);
                }
                _audio.PlayPvs(to.ClangSound, toUid, AudioParams.Default.WithVolume(-5f));
            }

            return true;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<DisposalHolderComponent>();
            while (query.MoveNext(out var uid, out var holder))
            {
                UpdateComp(uid, holder, frameTime);
            }
        }

        private void UpdateComp(EntityUid uid, DisposalHolderComponent holder, float frameTime)
        {
            while (frameTime > 0)
            {
                var time = frameTime;
                if (time > holder.TimeLeft)
                {
                    time = holder.TimeLeft;
                }

                holder.TimeLeft -= time;
                frameTime -= time;

                if (!EntityManager.EntityExists(holder.CurrentTube))
                {
                    ExitDisposals(uid, holder);
                    break;
                }

                var currentTube = holder.CurrentTube!.Value;
                if (holder.TimeLeft > 0)
                {
                    var progress = 1 - holder.TimeLeft / holder.StartingTime;
                    var origin = Transform(currentTube).Coordinates;
                    var destination = holder.CurrentDirection.ToVec();
                    var newPosition = destination * progress;

                    // This is some supreme shit code.
                    _transform.SetCoordinates(uid, origin.Offset(newPosition).WithEntityId(currentTube));
                    continue;
                }

                // Past this point, we are performing inter-tube transfer!
                // Remove current tube content
                Comp<DisposalTubeComponent>(currentTube).Contents.Remove(uid, reparent: false, force: true);

                // Find next tube
                var nextTube = _disposalTube.NextTubeFor(currentTube, holder.CurrentDirection);
                if (!EntityManager.EntityExists(nextTube))
                {
                    ExitDisposals(uid, holder);
                    break;
                }

                // Perform remainder of entry process
                if (!EnterTube(uid, nextTube!.Value, holder))
                {
                    break;
                }
            }
        }
    }
}
