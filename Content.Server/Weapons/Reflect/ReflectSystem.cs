using Content.Server.Administration.Logs;
using Content.Server.Hands.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Server.Popups;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;
using Content.Server.Weapons.Reflect;
using Content.Shared.Weapons.Ranged;
using Content.Server.Weapons.Melee.EnergySword;
using Content.Server.Projectiles;

namespace Server.Content.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public sealed class ReflectSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, ProjectileCollideAttemptEvent>(TryReflectProjectile);
        SubscribeLocalEvent<HitScanShotEvent>(TryReflectHitScan);

        SubscribeLocalEvent<ReflectComponent, EnergySwordActivatedEvent>(EnableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergySwordDeactivatedEvent>(DisableReflect);
    }

    private void TryReflectProjectile(EntityUid uid, ProjectileComponent projComp, ref ProjectileCollideAttemptEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physicsComp))
            return;
        if (TryComp<HandsComponent>(args.Target, out var hands))
        {
            foreach (var (_, hand) in hands.Hands)
            {
                if (TryComp<ReflectComponent>(hand.HeldEntity, out var reflect)
                    && reflect.Enabled
                    && _random.Prob(reflect.Chance))
                {
                    var vel = _physics.GetMapLinearVelocity(args.Target) - _physics.GetMapLinearVelocity(uid);
                    var spread = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2);
                    vel = spread.RotateVec(vel);
                    _physics.SetLinearVelocity(uid, vel);
                    _transform.SetWorldRotation(uid, vel.ToWorldAngle());
                    projComp.Shooter = args.Target;
                    _popup.PopupEntity(Loc.GetString("reflect-shot"), uid, PopupType.Small);
                    _audio.PlayPvs(reflect.OnReflect, uid, AudioHelpers.WithVariation(0.05f, _random));
                    _adminLogger.Add(LogType.ShotReflected, $"{ToPrettyString(args.Target):user} reflected projectile {ToPrettyString(uid):projectile}");
                    args.Cancelled = true;
                    return;
                }
            }
        }
    }

    private void TryReflectHitScan(ref HitScanShotEvent args)
    {
        if (args.User == null)
            return;
        if (TryComp<HandsComponent>(args.Target, out var hands))
        {
            foreach (var (_, hand) in hands.Hands)
            {
                if (TryComp<ReflectComponent>(hand.HeldEntity, out var reflect)
                    && reflect.Enabled
                    && _random.Prob(reflect.Chance))
                {
                    _popup.PopupEntity(Loc.GetString("reflect-shot"), args.Target, PopupType.Small);
                    _audio.PlayPvs(reflect.OnReflect, args.Target, AudioHelpers.WithVariation(0.05f, _random));
                    _adminLogger.Add(LogType.ShotReflected, $"{ToPrettyString(args.Target):entity} reflected hitscan shot");
                    args.Target = args.User.Value;
                    return;
                }
            }
        }
    }

    private void EnableReflect(EntityUid uid, ReflectComponent comp, ref EnergySwordActivatedEvent args)
    {
        comp.Enabled = true;
    }

    private void DisableReflect(EntityUid uid, ReflectComponent comp, ref EnergySwordDeactivatedEvent args)
    {
        comp.Enabled = false;
    }
}
