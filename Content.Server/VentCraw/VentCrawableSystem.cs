using System.Linq;
using Content.Shared.VentCraw.Tube.Components;
using Content.Shared.VentCraw.Components;
using Content.Shared.Body.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.VentCraw;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.VentCraw
{
    /// <summary>
    /// A system that handles the crawling behavior for vent creatures.
    /// </summary>
    public sealed class VentCrawableSystem : EntitySystem
    {
        [Dependency] private readonly VentCrawTubeSystem _ventCrawTubeSystem = default!;
        [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VentCrawHolderComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<VentCrawHolderComponent, MoveInputEvent>(OnMoveInput);
        }

        /// <summary>
        /// Handles the MoveInputEvent for VentCrawHolderComponent.
        /// </summary>
        /// <param name="uid">The EntityUid of the VentCrawHolderComponent.</param>
        /// <param name="component">The VentCrawHolderComponent instance.</param>
        /// <param name="args">The MoveInputEvent arguments.</param>
        private void OnMoveInput(EntityUid uid, VentCrawHolderComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<VentCrawHolderComponent>(uid, out var holder))
                return;

            if (!EntityManager.EntityExists(holder.CurrentTube))
            {
                ExitVentCraws(uid, holder);
            }

            component.IsMoving = args.State;
            component.CurrentDirection = args.Dir;
        }

        /// <summary>
        /// Handles the ComponentStartup event for VentCrawHolderComponent.
        /// </summary>
        /// <param name="uid">The EntityUid of the VentCrawHolderComponent.</param>
        /// <param name="holder">The VentCrawHolderComponent instance.</param>
        /// <param name="args">The ComponentStartup arguments.</param>
        private void OnComponentStartup(EntityUid uid, VentCrawHolderComponent holder, ComponentStartup args)
        {
            holder.Container = _containerSystem.EnsureContainer<Container>(uid, nameof(VentCrawHolderComponent));
        }

        /// <summary>
        /// Tries to insert an entity into the VentCrawHolderComponent container.
        /// </summary>
        /// <param name="uid">The EntityUid of the VentCrawHolderComponent.</param>
        /// <param name="toInsert">The EntityUid of the entity to insert.</param>
        /// <param name="holder">The VentCrawHolderComponent instance.</param>
        /// <returns>True if the insertion was successful, otherwise False.</returns>
        public bool TryInsert(EntityUid uid, EntityUid toInsert, VentCrawHolderComponent? holder = null)
        {
            if (!Resolve(uid, ref holder))
                return false;

            if (!CanInsert(uid, toInsert, holder))
                return false;

            if (!_containerSystem.Insert(toInsert, holder.Container))
                return false;

            if (TryComp<PhysicsComponent>(toInsert, out var physBody))
                _physicsSystem.SetCanCollide(toInsert, false, body: physBody);

            return true;
        }

        /// <summary>
        /// Checks whether the specified entity can be inserted into the container of the VentCrawHolderComponent.
        /// </summary>
        /// <param name="uid">The EntityUid of the VentCrawHolderComponent.</param>
        /// <param name="toInsert">The EntityUid of the entity to be inserted.</param>
        /// <param name="holder">The VentCrawHolderComponent instance.</param>
        /// <returns>True if the entity can be inserted into the container; otherwise, False.</returns>
        private bool CanInsert(EntityUid uid, EntityUid toInsert, VentCrawHolderComponent? holder = null)
        {
            if (!Resolve(uid, ref holder))
                return false;

            if (!_containerSystem.CanInsert(toInsert, holder.Container))
            {
                return false;
            }

            return HasComp<ItemComponent>(toInsert) ||
                   HasComp<BodyComponent>(toInsert);
        }

        /// <summary>
        /// Exits the vent craws for the specified VentCrawHolderComponent, removing it and any contained entities from the craws.
        /// </summary>
        /// <param name="uid">The EntityUid of the VentCrawHolderComponent.</param>
        /// <param name="holder">The VentCrawHolderComponent instance.</param>
        /// <param name="holderTransform">The TransformComponent instance for the VentCrawHolderComponent.</param>
        public void ExitVentCraws(EntityUid uid, VentCrawHolderComponent? holder = null, TransformComponent? holderTransform = null)
        {
            if (Terminating(uid))
                return;

            if (!Resolve(uid, ref holder, ref holderTransform))
                return;

            if (holder.IsExitingVentCraws)
            {
                Log.Error("Tried exiting VentCraws twice. This should never happen.");
                return;
            }

            holder.IsExitingVentCraws = true;

            foreach (var entity in holder.Container.ContainedEntities.ToArray())
            {
                RemComp<BeingVentCrawComponent>(entity);

                var meta = MetaData(entity);
                _containerSystem.Remove(entity, holder.Container, reparent: false, force: true);

                var xform = Transform(entity);
                if (xform.ParentUid != uid)
                    continue;

                _xformSystem.AttachToGridOrMap(entity, xform);

                if (TryComp<VentCrawlerComponent>(entity, out var ventCrawComp))
                {
                    ventCrawComp.InTube = false;
                    Dirty(entity , ventCrawComp);
                }

                if (EntityManager.TryGetComponent(entity, out PhysicsComponent? physics))
                {
                    _physicsSystem.WakeBody(entity, body: physics);
                }
            }

            EntityManager.DeleteEntity(uid);
        }

        /// <summary>
        /// Attempts to make the VentCrawHolderComponent enter a VentCrawTubeComponent.
        /// </summary>
        /// <param name="holderUid">The EntityUid of the VentCrawHolderComponent.</param>
        /// <param name="toUid">The EntityUid of the VentCrawTubeComponent to enter.</param>
        /// <param name="holder">The VentCrawHolderComponent instance.</param>
        /// <param name="holderTransform">The TransformComponent instance for the VentCrawHolderComponent.</param>
        /// <param name="to">The VentCrawTubeComponent instance to enter.</param>
        /// <param name="toTransform">The TransformComponent instance for the VentCrawTubeComponent.</param>
        /// <returns>True if the VentCrawHolderComponent successfully enters the VentCrawTubeComponent; otherwise, False.</returns>
        public bool EnterTube(EntityUid holderUid, EntityUid toUid, VentCrawHolderComponent? holder = null, TransformComponent? holderTransform = null, VentCrawTubeComponent? to = null, TransformComponent? toTransform = null)
        {
            if (!Resolve(holderUid, ref holder, ref holderTransform))
                return false;
            if (holder.IsExitingVentCraws)
            {
                Log.Error("Tried entering tube after exiting VentCraws. This should never happen.");
                return false;
            }
            if (!Resolve(toUid, ref to, ref toTransform))
            {
                ExitVentCraws(holderUid, holder, holderTransform);
                return false;
            }

            foreach (var ent in holder.Container.ContainedEntities)
            {
                var comp = EnsureComp<BeingVentCrawComponent>(ent);
                comp.Holder = holderUid;
            }

            if (!_containerSystem.Insert(holderUid, to.Contents))
            {
                ExitVentCraws(holderUid, holder, holderTransform);
                return false;
            }

            if (holder.CurrentTube != null)
            {
                holder.PreviousTube = holder.CurrentTube;
                holder.PreviousDirection = holder.CurrentDirection;
            }
            holder.CurrentTube = toUid;

            return true;
        }

        /// <summary>
        ///  Magic...
        /// </summary>
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<VentCrawHolderComponent>();
            while (query.MoveNext(out var uid, out var holder))
            {
                if (holder.CurrentDirection == Direction.Invalid)
                    return;

                var currentTube = holder.CurrentTube!.Value;

                if (holder.IsMoving && holder.NextTube == null)
                {
                    var nextTube = _ventCrawTubeSystem.NextTubeFor(currentTube, holder.CurrentDirection);

                    if (nextTube != null)
                    {
                        if (!EntityManager.EntityExists(holder.CurrentTube))
                        {
                            ExitVentCraws(uid, holder);
                            continue;
                        }

                        holder.NextTube = nextTube;
                        holder.StartingTime = holder.Speed;
                        holder.TimeLeft = holder.Speed;
                    }
                    else
                    {
                        var ev = new GetVentCrawsConnectableDirectionsEvent();
                        RaiseLocalEvent(currentTube, ref ev);
                        if (ev.Connectable.Contains(holder.CurrentDirection))
                        {
                            ExitVentCraws(uid, holder);
                            continue;
                        }
                    }
                }

                if (holder.NextTube != null && holder.TimeLeft > 0)
                {
                    var time = frameTime;
                    if (time > holder.TimeLeft)
                    {
                        time = holder.TimeLeft;
                    }

                    var progress = 1 - holder.TimeLeft / holder.StartingTime;
                    var origin = Transform(currentTube).Coordinates;
                    var target = Transform(holder.NextTube.Value).Coordinates;
                    var newPosition = (target.Position - origin.Position) * progress;

                    _xformSystem.SetCoordinates(uid, origin.Offset(newPosition).WithEntityId(currentTube));

                    holder.TimeLeft -= time;
                    frameTime -= time;
                }
                else if (holder.NextTube != null && holder.TimeLeft == 0)
                {
                    if (HasComp<VentCrawEntryComponent>(holder.NextTube.Value) && !holder.FirstEntry)
                    {
                        var welded = false;
                        if (TryComp<WeldableComponent>(holder.NextTube.Value, out var weldableComponent))
                        {
                            welded = weldableComponent.IsWelded;
                        }
                        if (!welded)
                        {
                            ExitVentCraws(uid, holder);
                        }
                    }
                    else
                    {
                        _containerSystem.Remove(uid, Comp<VentCrawTubeComponent>(currentTube).Contents ,reparent: false, force: true);

                        if (holder.FirstEntry)
                            holder.FirstEntry = false;

                        if (_gameTiming.CurTime > holder.LastCrawl + VentCrawHolderComponent.CrawlDelay)
                        {
                            holder.LastCrawl = _gameTiming.CurTime;
                            _audioSystem.PlayPvs(holder.CrawlSound, uid);
                        }

                        EnterTube(uid, holder.NextTube.Value, holder);
                        holder.NextTube = null;
                    }
                }
            }
        }
    }
}
