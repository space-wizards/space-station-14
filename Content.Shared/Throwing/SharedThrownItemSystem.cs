using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Gravity;
using Content.Shared.Physics;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing;

/// <summary>
///     Handles throwing landing and collisions.
/// </summary>
public abstract partial class SharedThrownItemSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private SharedBroadphaseSystem _broadphase = default!;
    [Dependency] protected SharedPhysicsSystem Physics = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;

    private const string ThrowingFixture = "throw-fixture";

    public override void Initialize()
    {
        SubscribeLocalEvent<ThrownItemComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThrownItemComponent, PhysicsSleepEvent>(OnSleep);
        SubscribeLocalEvent<ThrownItemComponent, StartCollideEvent>(HandleCollision);
        SubscribeLocalEvent<ThrownItemComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<ThrownItemComponent, ThrownEvent>(ThrowItem);

        SubscribeLocalEvent<PullStartedMessage>(HandlePullStarted);
    }

    private void OnMapInit(Entity<ThrownItemComponent> thrown, ref MapInitEvent args)
    {
        thrown.Comp.ThrownTime ??= _gameTiming.CurTime;
        Dirty(thrown);
    }

    private void ThrowItem(Entity<ThrownItemComponent> thrown, ref ThrownEvent args)
    {
        if (!TryComp(thrown, out FixturesComponent? fixturesComponent)
            || fixturesComponent.Fixtures.Count != 1
            || !TryComp<PhysicsComponent>(thrown, out var body))
        {
            return;
        }

        var fixture = fixturesComponent.Fixtures.Values.First();
        var shape = fixture.Shape;
        _fixtures.TryCreateFixture(thrown, shape, ThrowingFixture, hard: false, collisionMask: (int) CollisionGroup.ThrownItem, manager: fixturesComponent, body: body);
    }

    private void HandleCollision(Entity<ThrownItemComponent> thrown, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        if (args.OtherEntity == thrown.Comp.Thrower)
            return;

        ThrowCollideInteraction(thrown, args.OtherEntity);
    }

    private void PreventCollision(Entity<ThrownItemComponent> thrown, ref PreventCollideEvent args)
    {
        if (args.OtherEntity == thrown.Comp.Thrower)
        {
            args.Cancelled = true;
        }
    }

    private void OnSleep(Entity<ThrownItemComponent> thrown, ref PhysicsSleepEvent @event)
    {
        StopThrow(thrown);
        // Physics.UpdateIsPredicted(thrown);
    }

    private void HandlePullStarted(PullStartedMessage message)
    {
        // TODO: this isn't directed so things have to be done the bad way
        if (TryComp(message.PulledUid, out ThrownItemComponent? thrownItemComponent))
            StopThrow((message.PulledUid, thrownItemComponent));
    }

    public void StopThrow(Entity<ThrownItemComponent> thrown)
    {
        if (TryComp<PhysicsComponent>(thrown, out var physics))
        {
            Physics.SetBodyStatus(thrown, physics, BodyStatus.OnGround);

            if (physics.Awake)
                _broadphase.RegenerateContacts((thrown, physics));
        }

        if (TryComp(thrown, out FixturesComponent? manager))
        {
            var fixture = _fixtures.GetFixtureOrNull(thrown, ThrowingFixture, manager: manager);

            if (fixture != null)
            {
                _fixtures.DestroyFixture(thrown, ThrowingFixture, fixture, manager: manager);
            }
        }

        var ev = new StopThrowEvent(thrown.Comp.Thrower);
        RaiseLocalEvent(thrown, ref ev);
        RemCompDeferred<ThrownItemComponent>(thrown);
    }

    public void LandComponent(Entity<ThrownItemComponent> thrown, PhysicsComponent physics, bool playSound)
    {
        if (thrown.Comp.Landed || thrown.Comp.Deleted || _gravity.IsWeightless(thrown.Owner) || Deleted(thrown))
            return;

        thrown.Comp.Landed = true;

        // Assume it's uninteresting if it has no thrower. For now anyway.
        if (thrown.Comp.Thrower is not null)
            _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(thrown):entity} thrown by {ToPrettyString(thrown.Comp.Thrower.Value):thrower} landed.");

        _broadphase.RegenerateContacts((thrown, physics));
        var landEvent = new LandEvent(thrown.Comp.Thrower, playSound);
        RaiseLocalEvent(thrown, ref landEvent);
    }

    /// <summary>
    ///     Raises collision events on the thrown and target entities.
    /// </summary>
    public void ThrowCollideInteraction(Entity<ThrownItemComponent> thrown, EntityUid target)
    {
        if (thrown.Comp.Thrower is not null)
            _adminLogger.Add(LogType.ThrowHit, LogImpact.Low,
                $"{ToPrettyString(thrown):thrown} thrown by {ToPrettyString(thrown.Comp.Thrower.Value):thrower} hit {ToPrettyString(target):target}.");

        var hitByEv = new ThrowHitByEvent(thrown, target);
        var doHitEv = new ThrowDoHitEvent(thrown, target);
        RaiseLocalEvent(target, ref hitByEv, true);
        RaiseLocalEvent(thrown, ref doHitEv, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ThrownItemComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var thrown, out var physics))
        {
            // If you remove this check verify slipping for other entities is networked properly.
            if (_netMan.IsClient && !physics.Predict)
                continue;

            if (thrown.LandTime <= _gameTiming.CurTime)
            {
                LandComponent((uid, thrown), physics, thrown.PlayLandSound);
            }

            var stopThrowTime = thrown.LandTime ?? thrown.ThrownTime;
            if (stopThrowTime <= _gameTiming.CurTime)
            {
                StopThrow((uid, thrown));
            }
        }
    }
}
