using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public abstract class SharedReflectSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, ProjectileReflectAttemptEvent>(OnHandReflectProjectile);
        SubscribeLocalEvent<HandsComponent, HitScanReflectAttemptEvent>(OnHandsReflectHitscan);

        SubscribeLocalEvent<ReflectComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ReflectComponent, ComponentGetState>(OnGetState);
    }

    private static void OnHandleState(EntityUid uid, ReflectComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ReflectComponentState state) return;
        component.Enabled = state.Enabled;
        component.EnergeticChance = state.EnergeticChance;
        component.KineticChance = state.KineticChance;
        component.Spread = state.Spread;
    }

    private static void OnGetState(EntityUid uid, ReflectComponent component, ref ComponentGetState args)
    {
        args.State = new ReflectComponentState(component.Enabled, component.EnergeticChance, component.KineticChance, component.Spread);
    }

    private void OnHandReflectProjectile(EntityUid uid, HandsComponent hands, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (TryReflectProjectile(uid, hands.ActiveHandEntity, args.ProjUid, args.Component))
            args.Cancelled = true;
    }
    
    private bool TryReflectProjectile(EntityUid user, EntityUid? reflector, EntityUid projectile, ProjectileComponent component)
    {
        var isEnergyProjectile = component.Damage.DamageDict.ContainsKey("Heat");
        var isKineticProjectile = !isEnergyProjectile;
        if (TryComp<ReflectComponent>(reflector, out var reflect) &&
            reflect.Enabled && 
            (isEnergyProjectile && _random.Prob(reflect.EnergeticChance) || isKineticProjectile && _random.Prob(reflect.KineticChance)))
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

    private void OnHandsReflectHitscan(EntityUid uid, HandsComponent hands, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;
        if (TryReflectHitscan(uid, hands.ActiveHandEntity, args.Direction, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private bool TryReflectHitscan(EntityUid user, EntityUid? reflector, Vector2 direction, [NotNullWhen(true)] out Vector2? newDirection)
    {
        if (TryComp<ReflectComponent>(reflector, out var reflect) &&
            reflect.Enabled &&
            _random.Prob(reflect.EnergeticChance))
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
