using System.Linq;
 using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    [UsedImplicitly]
    internal sealed class DisposableSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DisposalUnitSystem _disposalUnitSystem = default!;
        [Dependency] private readonly DisposalTubeSystem _disposalTubeSystem = default!;

        public void ExitDisposals(EntityUid uid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null)
        {
            if (!Resolve(uid, ref holder, ref holderTransform))
                return;
            if (holder.IsExitingDisposals)
            {
                Logger.ErrorS("c.s.disposal.holder", "Tried exiting disposals twice. This should never happen.");
                return;
            }
            holder.IsExitingDisposals = true;

            // Check for a disposal unit to throw them into and then eject them from it.
            // *This ejection also makes the target not collide with the unit.*
            // *This is on purpose.*
            var grid = _mapManager.GetGrid(holderTransform.GridID);
            var gridTileContents = grid.GetLocal(holderTransform.Coordinates);
            DisposalUnitComponent? duc = null;
            foreach (var contentUid in gridTileContents)
            {
                if (EntityManager.TryGetComponent(contentUid, out duc))
                    break;
            }

            foreach (var entity in holder.Container.ContainedEntities.ToArray())
            {
                if (HasComp<BeingDisposedComponent>(entity))
                    RemComp <BeingDisposedComponent>(entity);

                if (EntityManager.TryGetComponent(entity, out IPhysBody? physics))
                {
                    physics.CanCollide = true;
                }

                holder.Container.ForceRemove(entity);

                if (EntityManager.GetComponent<TransformComponent>(entity).Parent == holderTransform)
                {
                    if (duc != null)
                    {
                        // Insert into disposal unit
                        EntityManager.GetComponent<TransformComponent>(entity).Coordinates = new EntityCoordinates((duc).Owner, Vector2.Zero);
                        duc.Container.Insert(entity);
                    }
                    else
                    {
                        EntityManager.GetComponent<TransformComponent>(entity).AttachParentToContainerOrGrid();
                    }
                }
            }

            if (duc != null)
            {
                _disposalUnitSystem.TryEjectContents(duc);
            }

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            if (atmosphereSystem.GetTileMixture(holderTransform.Coordinates, true) is {} environment)
            {
                atmosphereSystem.Merge(environment, holder.Air);
                holder.Air.Clear();
            }

            EntityManager.DeleteEntity(uid);
        }

        // Note: This function will cause an ExitDisposals on any failure that does not make an ExitDisposals impossible.
        public bool EnterTube(EntityUid holderUid, EntityUid toUid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null, IDisposalTubeComponent? to = null, TransformComponent? toTransform = null)
        {
            if (!Resolve(holderUid, ref holder, ref holderTransform))
                return false;
            if (holder.IsExitingDisposals)
            {
                Logger.ErrorS("c.s.disposal.holder", "Tried entering tube after exiting disposals. This should never happen.");
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
                comp.Holder = holder.Owner;
            }

            // Insert into next tube
            holderTransform.Coordinates = new EntityCoordinates(toUid, Vector2.Zero);
            if (!to.Contents.Insert(holder.Owner))
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            if (holder.CurrentTube != null)
            {
                holder.PreviousTube = holder.CurrentTube;
                holder.PreviousDirection = holder.CurrentDirection;
            }
            holderTransform.Coordinates = toTransform.Coordinates;
            holder.CurrentTube = to;
            holder.CurrentDirection = to.NextDirection(holder);
            holder.StartingTime = 0.1f;
            holder.TimeLeft = 0.1f;
            // Logger.InfoS("c.s.disposal.holder", $"Disposals dir {holder.CurrentDirection}");

            // Invalid direction = exit now!
            if (holder.CurrentDirection == Direction.Invalid)
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }
            return true;
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<DisposalHolderComponent>())
            {
                UpdateComp(comp, frameTime);
            }
        }

        private void UpdateComp(DisposalHolderComponent holder, float frameTime)
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

                var currentTube = holder.CurrentTube;
                if (currentTube == null || currentTube.Deleted)
                {
                    ExitDisposals((holder).Owner);
                    break;
                }

                if (holder.TimeLeft > 0)
                {
                    var progress = 1 - holder.TimeLeft / holder.StartingTime;
                    var origin = EntityManager.GetComponent<TransformComponent>(currentTube.Owner).Coordinates;
                    var destination = holder.CurrentDirection.ToVec();
                    var newPosition = destination * progress;

                    EntityManager.GetComponent<TransformComponent>(holder.Owner).Coordinates = origin.Offset(newPosition);

                    continue;
                }

                // Past this point, we are performing inter-tube transfer!
                // Remove current tube content
                currentTube.Contents.ForceRemove(holder.Owner);

                // Find next tube
                var nextTube = _disposalTubeSystem.NextTubeFor(currentTube.Owner, holder.CurrentDirection);
                if (nextTube == null || nextTube.Deleted)
                {
                    ExitDisposals((holder).Owner);
                    break;
                }

                // Perform remainder of entry process
                if (!EnterTube((holder).Owner, nextTube.Owner, holder, null, nextTube, null))
                {
                    break;
                }
            }
        }
    }
}
