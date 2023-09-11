using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Handles throwing landing and collisions.
    /// </summary>
    public sealed class ThrownItemSystem : EntitySystem
    {
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        private const string ThrowingFixture = "throw-fixture";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrownItemComponent, PhysicsSleepEvent>(OnSleep);
            SubscribeLocalEvent<ThrownItemComponent, StartCollideEvent>(HandleCollision);
            SubscribeLocalEvent<ThrownItemComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<ThrownItemComponent, ThrownEvent>(ThrowItem);
            SubscribeLocalEvent<ThrownItemComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<ThrownItemComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<PullStartedMessage>(HandlePullStarted);
        }

        private void OnGetState(EntityUid uid, ThrownItemComponent component, ref ComponentGetState args)
        {
            args.State = new ThrownItemComponentState(GetNetEntity(component.Thrower));
        }

        private void OnHandleState(EntityUid uid, ThrownItemComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ThrownItemComponentState { Thrower: not null } state ||
                !state.Thrower.Value.IsValid())
            {
                return;
            }

            component.Thrower = EnsureEntity<ThrownItemComponent>(state.Thrower.Value, uid);
        }

        private void ThrowItem(EntityUid uid, ThrownItemComponent component, ThrownEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out FixturesComponent? fixturesComponent) ||
                fixturesComponent.Fixtures.Count != 1 ||
                !TryComp<PhysicsComponent>(uid, out var body))
            {
                return;
            }

            var fixture = fixturesComponent.Fixtures.Values.First();
            var shape = fixture.Shape;
            _fixtures.TryCreateFixture(uid, shape, ThrowingFixture, hard: false, collisionMask: (int) CollisionGroup.ThrownItem, manager: fixturesComponent, body: body);
        }

        private void HandleCollision(EntityUid uid, ThrownItemComponent component, ref StartCollideEvent args)
        {
            if (!args.OtherFixture.Hard)
                return;

            if (args.OtherEntity == component.Thrower)
                return;

            ThrowCollideInteraction(component, args.OurEntity, args.OtherEntity);
        }

        private void PreventCollision(EntityUid uid, ThrownItemComponent component, ref PreventCollideEvent args)
        {
            if (args.OtherEntity == component.Thrower)
            {
                args.Cancelled = true;
            }
        }

        private void OnSleep(EntityUid uid, ThrownItemComponent thrownItem, ref PhysicsSleepEvent @event)
        {
            StopThrow(uid, thrownItem);
        }

        private void HandlePullStarted(PullStartedMessage message)
        {
            // TODO: this isn't directed so things have to be done the bad way
            if (EntityManager.TryGetComponent(message.Pulled.Owner, out ThrownItemComponent? thrownItemComponent))
                StopThrow(message.Pulled.Owner, thrownItemComponent);
        }

        public void StopThrow(EntityUid uid, ThrownItemComponent thrownItemComponent)
        {
            if (TryComp<PhysicsComponent>(uid, out var physics))
            {
                _physics.SetBodyStatus(physics, BodyStatus.OnGround);
            }

            if (EntityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                var fixture = _fixtures.GetFixtureOrNull(uid, ThrowingFixture, manager: manager);

                if (fixture != null)
                {
                    _fixtures.DestroyFixture(uid, ThrowingFixture, fixture, manager: manager);
                }
            }

            EntityManager.EventBus.RaiseLocalEvent(uid, new StopThrowEvent {User = thrownItemComponent.Thrower}, true);
            EntityManager.RemoveComponent<ThrownItemComponent>(uid);
        }

        public void LandComponent(EntityUid uid, ThrownItemComponent thrownItem, PhysicsComponent physics, bool playSound)
        {
            if (thrownItem.Deleted || Deleted(uid))
                return;

            // Assume it's uninteresting if it has no thrower. For now anyway.
            if (thrownItem.Thrower is not null)
                _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(uid):entity} thrown by {ToPrettyString(thrownItem.Thrower.Value):thrower} landed.");

            _broadphase.RegenerateContacts(uid, physics);
            var landEvent = new LandEvent(thrownItem.Thrower, playSound);
            RaiseLocalEvent(uid, ref landEvent);

            StopThrow(uid, thrownItem);
        }

        /// <summary>
        ///     Raises collision events on the thrown and target entities.
        /// </summary>
        public void ThrowCollideInteraction(ThrownItemComponent component, EntityUid thrown, EntityUid target)
        {
            if (component.Thrower is not null)
                _adminLogger.Add(LogType.ThrowHit, LogImpact.Low,
                    $"{ToPrettyString(thrown):thrown} thrown by {ToPrettyString(component.Thrower.Value):thrower} hit {ToPrettyString(target):target}.");

            RaiseLocalEvent(target, new ThrowHitByEvent(thrown, target, component), true);
            RaiseLocalEvent(thrown, new ThrowDoHitEvent(thrown, target, component), true);
        }
    }
}
