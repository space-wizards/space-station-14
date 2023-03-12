using Content.Shared.Audio;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public abstract class SharedReflectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedHandsComponent, ProjectileReflectAttemptEvent>(OnHandReflectProjectile);
        SubscribeLocalEvent<SharedHandsComponent, HitScanReflectAttemptEvent>(OnHandsReflectHitScan);
        SubscribeLocalEvent<ReflectComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ReflectComponent, ComponentGetState>(OnGetState);
    }

    private static void OnHandleState(EntityUid uid, ReflectComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ReflectComponentState state) return;
        component.Enabled = state.Enabled;
        component.Chance = state.Chance;
        component.Spread = state.Spread;
    }

    private static void OnGetState(EntityUid uid, ReflectComponent component, ref ComponentGetState args)
    {
        args.State = new ReflectComponentState(component.Enabled, component.Chance, component.Spread);
    }

    private void OnHandReflectProjectile(EntityUid uid, SharedHandsComponent hands, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (TryReflectProjectile(uid, hands.ActiveHandEntity, args.ProjUid))
            args.Cancelled = true;
    }
    
    public bool TryReflectProjectile(EntityUid user, EntityUid? reflector, EntityUid projectile)
    {
        if (TryComp<ReflectComponent>(reflector, out var reflect) &&
            reflect.Enabled && 
            _random.Prob(reflect.Chance))
        {
            var rotation = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2).Opposite();

            var relVel = _physics.GetMapLinearVelocity(projectile) - _physics.GetMapLinearVelocity(user);
            var newVel = rotation.RotateVec(relVel);
            _physics.SetLinearVelocity(projectile, newVel);

            var locRot = Transform(projectile).LocalRotation;
            var newRot = rotation.RotateVec(locRot.ToVec());
            _transform.SetLocalRotation(projectile, newRot.ToAngle());

            _popup.PopupEntity(Loc.GetString("reflect-shot"), user, PopupType.Small);
            _audio.PlayPvs(reflect.OnReflect, user, AudioHelpers.WithVariation(0.05f, _random));
            return true;
        }
        return false;
    }

    private void OnHandsReflectHitScan(EntityUid uid, SharedHandsComponent hands, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;
        if (TryReflectHitScan(uid, hands.ActiveHandEntity, args.Direction, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    public bool TryReflectHitScan(EntityUid user, EntityUid? reflector, Vector2 direction, [NotNullWhen(true)] out Vector2? newDirection)
    {
        if (TryComp<ReflectComponent>(reflector, out var reflect) &&
            reflect.Enabled &&
            _random.Prob(reflect.Chance))
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), user, PopupType.Small);
            _audio.PlayPvs(reflect.OnReflect, user, AudioHelpers.WithVariation(0.05f, _random));
            var spread = _random.NextAngle(-reflect.Spread / 2, reflect.Spread / 2);
            newDirection = -spread.RotateVec(direction);
            return true;
        }
        newDirection = null;
        return false;
    }
}
