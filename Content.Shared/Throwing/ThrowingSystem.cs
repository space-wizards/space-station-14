using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Camera;
using Content.Shared.Database;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing;

public sealed class ThrowingSystem : EntitySystem
{
    public const float ThrowAngularImpulse = 5f;

    public const float PushbackDefault = 2f;

    /// <summary>
    /// The minimum amount of time an entity needs to be thrown before the timer can be run.
    /// Anything below this threshold never enters the air.
    /// </summary>
    public const float FlyTime = 0.15f;

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrownItemSystem _thrownSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public void TryThrow(
        EntityUid uid,
        EntityCoordinates coordinates,
        float strength = 1.0f,
        EntityUid? user = null,
        float pushbackRatio = PushbackDefault,
        bool playSound = true)
    {
        var thrownPos = Transform(uid).MapPosition;
        var mapPos = coordinates.ToMap(EntityManager, _transform);

        if (mapPos.MapId != thrownPos.MapId)
            return;

        TryThrow(uid, mapPos.Position - thrownPos.Position, strength, user, pushbackRatio, playSound);
    }

    /// <summary>
    ///     Tries to throw the entity if it has a physics component, otherwise does nothing.
    /// </summary>
    /// <param name="uid">The entity being thrown.</param>
    /// <param name="direction">A vector pointing from the entity to its destination.</param>
    /// <param name="strength">How much the direction vector should be multiplied for velocity.</param>
    /// <param name="pushbackRatio">The ratio of impulse applied to the thrower - defaults to 10 because otherwise it's not enough to properly recover from getting spaced</param>
    public void TryThrow(EntityUid uid,
        Vector2 direction,
        float strength = 1.0f,
        EntityUid? user = null,
        float pushbackRatio = PushbackDefault,
        bool playSound = true)
    {
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        if (!physicsQuery.TryGetComponent(uid, out var physics))
            return;

        var projectileQuery = GetEntityQuery<ProjectileComponent>();
        var tagQuery = GetEntityQuery<TagComponent>();

        TryThrow(
            uid,
            direction,
            physics,
            Transform(uid),
            projectileQuery,
            strength,
            user,
            pushbackRatio,
            playSound);
    }

    /// <summary>
    ///     Tries to throw the entity if it has a physics component, otherwise does nothing.
    /// </summary>
    /// <param name="uid">The entity being thrown.</param>
    /// <param name="direction">A vector pointing from the entity to its destination.</param>
    /// <param name="strength">How much the direction vector should be multiplied for velocity.</param>
    /// <param name="pushbackRatio">The ratio of impulse applied to the thrower - defaults to 10 because otherwise it's not enough to properly recover from getting spaced</param>
    public void TryThrow(EntityUid uid,
        Vector2 direction,
        PhysicsComponent physics,
        TransformComponent transform,
        EntityQuery<ProjectileComponent> projectileQuery,
        float strength = 1.0f,
        EntityUid? user = null,
        float pushbackRatio = PushbackDefault,
        bool playSound = true)
    {
        if (strength <= 0 || direction == Vector2Helpers.Infinity || direction == Vector2Helpers.NaN || direction == Vector2.Zero)
            return;

        if ((physics.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0)
        {
            Log.Warning($"Tried to throw entity {ToPrettyString(uid)} but can't throw {physics.BodyType} bodies!");
            return;
        }

        // Allow throwing if this projectile only acts as a projectile when shot, otherwise disallow
        if (projectileQuery.TryGetComponent(uid, out var proj) && !proj.OnlyCollideWhenShot)
            return;

        var comp = new ThrownItemComponent();
        comp.Thrower = user;

        // Estimate time to arrival so we can apply OnGround status and slow it much faster.
        var time = direction.Length() / strength;
        comp.ThrownTime = _gameTiming.CurTime;
        // did we launch this with something stronger than our hands?
        if (TryComp<HandsComponent>(comp.Thrower, out var hands) && strength > hands.ThrowForceMultiplier)
            comp.LandTime = comp.ThrownTime + TimeSpan.FromSeconds(time);
        else
            comp.LandTime = time < FlyTime ? default : comp.ThrownTime + TimeSpan.FromSeconds(time - FlyTime);
        comp.PlayLandSound = playSound;
        AddComp(uid, comp, true);

        ThrowingAngleComponent? throwingAngle = null;

        // Give it a l'il spin.
        if (physics.InvI > 0f && (!TryComp(uid, out throwingAngle) || throwingAngle.AngularVelocity))
        {
            _physics.ApplyAngularImpulse(uid, ThrowAngularImpulse / physics.InvI, body: physics);
        }
        else
        {
            Resolve(uid, ref throwingAngle, false);
            var gridRot = _transform.GetWorldRotation(transform.ParentUid);
            var angle = direction.ToWorldAngle() - gridRot;
            var offset = throwingAngle?.Angle ?? Angle.Zero;
            _transform.SetLocalRotation(uid, angle + offset);
        }

        var throwEvent = new ThrownEvent(user, uid);
        RaiseLocalEvent(uid, ref throwEvent, true);
        if (user != null)
            _adminLogger.Add(LogType.Throw, LogImpact.Low, $"{ToPrettyString(user.Value):user} threw {ToPrettyString(uid):entity}");

        var impulseVector = direction.Normalized() * strength * physics.Mass;
        _physics.ApplyLinearImpulse(uid, impulseVector, body: physics);

        if (comp.LandTime == null || comp.LandTime <= TimeSpan.Zero)
        {
            _thrownSystem.LandComponent(uid, comp, physics, playSound);
        }
        else
        {
            _physics.SetBodyStatus(physics, BodyStatus.InAir);
        }

        if (user == null)
            return;

        _recoil.KickCamera(user.Value, -direction * 0.3f);

        // Give thrower an impulse in the other direction
        if (pushbackRatio != 0.0f &&
            physics.Mass > 0f &&
            TryComp(user.Value, out PhysicsComponent? userPhysics) &&
            _gravity.IsWeightless(user.Value, userPhysics))
        {
            var msg = new ThrowPushbackAttemptEvent();
            RaiseLocalEvent(uid, msg);
            const float MassLimit = 5f;

            if (!msg.Cancelled)
                _physics.ApplyLinearImpulse(user.Value, -impulseVector / physics.Mass * pushbackRatio * MathF.Min(MassLimit, physics.Mass), body: userPhysics);
        }
    }
}
