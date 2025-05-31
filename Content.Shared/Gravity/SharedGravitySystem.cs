using Content.Shared.Alert;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Gravity
{
    public abstract partial class SharedGravitySystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;

        [ValidatePrototypeId<AlertPrototype>]
        public const string WeightlessAlert = "Weightless";

        private EntityQuery<GravityComponent> _gravityQuery;
        protected EntityQuery<TransformComponent> XformQuery;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<AlertSyncEvent>(OnAlertsSync);
            SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);
            SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);
            SubscribeLocalEvent<GravityComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<WeightlessnessComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<WeightlessnessComponent, EntParentChangedMessage>(OnEntParentChanged);
            SubscribeLocalEvent<WeightlessnessComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);

            _gravityQuery = GetEntityQuery<GravityComponent>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateShake();
        }

        [Obsolete("Use the Entity<WeightlessnessComponent?> overload instead.")]
        public bool IsWeightless(EntityUid uid, PhysicsComponent body, TransformComponent? xform = null)
        {
            if (TryComp<WeightlessnessComponent>(uid, out var weightless))
                return IsWeightless((uid, weightless));

            if ((body.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
                return false;

            var ev = new IsWeightlessEvent();
            RaiseLocalEvent(uid, ref ev);
            if (ev.Handled)
                return ev.IsWeightless;

            if (!Resolve(uid, ref xform))
                return true;

            // If grid / map has gravity
            if (EntityGridOrMapHaveGravity((uid, xform)))
                return false;

            return true;
        }

        public bool IsWeightless(Entity<WeightlessnessComponent?> entity)
        {
            // If we can be weightless and are weightless, return true, otherwise return false
            return Resolve(entity, ref entity.Comp, false) && entity.Comp.Weightless;
        }

        private bool TryWeightless(Entity<WeightlessnessComponent?, PhysicsComponent?, TransformComponent?> entity)
        {
            if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2, false))
                return false;

            if (entity.Comp2.BodyType is BodyType.Static or BodyType.Kinematic)
                return false;

            if (!Resolve(entity, ref entity.Comp3))
                return true;

            // Check if something other than the grid or map is overriding our gravity
            var ev = new IsWeightlessEvent();
            RaiseLocalEvent(entity, ref ev);
            if (ev.Handled)
                return ev.IsWeightless;

            return !EntityGridOrMapHaveGravity((entity, entity.Comp3));
        }

        /// <summary>
        /// Refreshes weightlessness status, needs to be called anytime it would change.
        /// </summary>
        /// <param name="entity">The entity we are updating the weightless status of</param>
        /// <param name="weightless">The weightless value we are trying to change to, helps avoid networking</param>
        public void RefreshWeightless(Entity<WeightlessnessComponent?> entity, bool? weightless = null)
        {
            if (!Resolve(entity, ref entity.Comp))
                return;

            // Only update if we're changing our weightless status
            if (entity.Comp.Weightless == weightless)
                return;

            entity.Comp.Weightless = TryWeightless(entity);
            Dirty(entity);
        }

        private void OnMapInit(Entity<WeightlessnessComponent> entity, ref MapInitEvent args)
        {
            RefreshWeightless((entity.Owner, entity.Comp));
        }

        private void OnEntParentChanged(Entity<WeightlessnessComponent> entity, ref EntParentChangedMessage args)
        {
            // If we've moved but are still on the same grid, then don't do anything.
            if (args.OldParent == args.Transform.GridUid)
                return;

            RefreshWeightless((entity.Owner, entity.Comp), !EntityGridOrMapHaveGravity((entity, args.Transform)));
        }

        private void OnBodyTypeChanged(Entity<WeightlessnessComponent> entity, ref PhysicsBodyTypeChangedEvent args)
        {
            // No need to update weightlessness if we're not weightless and we're a body type that can't be weightless
            if (args.New is BodyType.Static or BodyType.Kinematic && entity.Comp.Weightless == false)
                return;

            RefreshWeightless((entity.Owner, entity.Comp));
        }

        /// <summary>
        /// Checks if a given entity is currently standing on a grid or map that supports having gravity at all.
        /// </summary>
        public bool EntityOnGravitySupportingGridOrMap(Entity<TransformComponent?> entity)
        {
            entity.Comp ??= Transform(entity);

            return _gravityQuery.HasComp(entity.Comp.GridUid) ||
                   _gravityQuery.HasComp(entity.Comp.MapUid);
        }

        /// <summary>
        /// Checks if a given entity is currently standing on a grid or map that has gravity of some kind.
        /// </summary>
        public bool EntityGridOrMapHaveGravity(Entity<TransformComponent?> entity)
        {
            entity.Comp ??= Transform(entity);

            return _gravityQuery.TryComp(entity.Comp.GridUid, out var gravity) && gravity.Enabled ||
                   _gravityQuery.TryComp(entity.Comp.MapUid, out var mapGravity) && mapGravity.Enabled;
        }

        private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not GravityComponentState state)
                return;

            if (component.EnabledVV == state.Enabled)
                return;
            component.EnabledVV = state.Enabled;
            var ev = new GravityChangedEvent(uid, component.EnabledVV);
            RaiseLocalEvent(uid, ref ev, true);
        }

        private void OnGetState(EntityUid uid, GravityComponent component, ref ComponentGetState args)
        {
            args.State = new GravityComponentState(component.EnabledVV);
        }

        private void OnGravityChange(ref GravityChangedEvent ev)
        {
            var gravity = AllEntityQuery<WeightlessnessComponent, TransformComponent>();
            while(gravity.MoveNext(out var uid, out var weightless, out var xform))
            {
                if (xform.GridUid != ev.ChangedGridIndex || ev.HasGravity == !weightless.Weightless )
                    continue;

                weightless.Weightless = TryWeightless(uid);
                Dirty(uid, weightless);

                if(!HasComp<AlertsComponent>(uid))
                    continue;

                if (weightless.Weightless)
                {
                    _alerts.ShowAlert(uid, WeightlessAlert);
                }
                else
                {
                    _alerts.ClearAlert(uid, WeightlessAlert);
                }
            }
        }

        private void OnAlertsSync(AlertSyncEvent ev)
        {
            if (IsWeightless(ev.Euid))
            {
                _alerts.ShowAlert(ev.Euid, WeightlessAlert);
            }
            else
            {
                _alerts.ClearAlert(ev.Euid, WeightlessAlert);
            }
        }

        private void OnAlertsParentChange(EntityUid uid, AlertsComponent component, ref EntParentChangedMessage args)
        {
            if (IsWeightless(uid))
            {
                _alerts.ShowAlert(uid, WeightlessAlert);
            }
            else
            {
                _alerts.ClearAlert(uid, WeightlessAlert);
            }
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<GravityComponent>(ev.EntityUid);
        }

        [Serializable, NetSerializable]
        private sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }

    [ByRefEvent]
    public record struct IsWeightlessEvent(bool IsWeightless = false, bool Handled = false) : IInventoryRelayEvent
    {
        SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
    }
}
