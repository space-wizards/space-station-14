using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// This handles reflecting projectiles and hitscan shots.
/// </summary>
public sealed class ReflectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<ReflectComponent, ProjectileReflectAttemptEvent>(OnReflectUserCollide, baseEvent: false);
        Subs.SubscribeWithRelay<ReflectComponent, HitScanReflectAttemptEvent>(OnReflectUserHitscan, baseEvent: false);
        SubscribeLocalEvent<ReflectComponent, ProjectileReflectAttemptEvent>(OnReflectCollide);
        SubscribeLocalEvent<ReflectComponent, HitScanReflectAttemptEvent>(OnReflectHitscan);

        SubscribeLocalEvent<ReflectComponent, GotEquippedEvent>(OnReflectEquipped);
        SubscribeLocalEvent<ReflectComponent, GotUnequippedEvent>(OnReflectUnequipped);
        SubscribeLocalEvent<ReflectComponent, GotEquippedHandEvent>(OnReflectHandEquipped);
        SubscribeLocalEvent<ReflectComponent, GotUnequippedHandEvent>(OnReflectHandUnequipped);
    }

    private void OnReflectUserCollide(Entity<ReflectComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.InRightPlace)
            return; // only reflect when equipped correctly

        if (TryReflectProjectile(ent, ent.Owner, args.ProjUid))
            args.Cancelled = true;
    }

    private void OnReflectUserHitscan(Entity<ReflectComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;

        if (!ent.Comp.InRightPlace)
            return; // only reflect when equipped correctly

        if (TryReflectHitscan(ent, ent.Owner, args.Shooter, args.SourceItem, args.Direction, args.Reflective, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private void OnReflectCollide(Entity<ReflectComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryReflectProjectile(ent, ent.Owner, args.ProjUid))
            args.Cancelled = true;
    }

    private void OnReflectHitscan(Entity<ReflectComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;

        if (TryReflectHitscan(ent, ent.Owner, args.Shooter, args.SourceItem, args.Direction, args.Reflective, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private bool TryReflectProjectile(Entity<ReflectComponent> reflector, EntityUid user, Entity<ProjectileComponent?> projectile)
    {
        if (!TryComp<ReflectiveComponent>(projectile, out var reflective) ||
            (reflector.Comp.Reflects & reflective.Reflective) == 0x0 ||
            !_toggle.IsActivated(reflector.Owner) ||
            !_random.Prob(reflector.Comp.ReflectProb) ||
            !TryComp<PhysicsComponent>(projectile, out var physics))
        {
            return false;
        }

        var rotation = _random.NextAngle(-reflector.Comp.Spread / 2, reflector.Comp.Spread / 2).Opposite();
        var existingVelocity = _physics.GetMapLinearVelocity(projectile, component: physics);
        var relativeVelocity = existingVelocity - _physics.GetMapLinearVelocity(user);
        var newVelocity = rotation.RotateVec(relativeVelocity);

        // Have the velocity in world terms above so need to convert it back to local.
        var difference = newVelocity - existingVelocity;

        _physics.SetLinearVelocity(projectile, physics.LinearVelocity + difference, body: physics);

        var locRot = Transform(projectile).LocalRotation;
        var newRot = rotation.RotateVec(locRot.ToVec());
        _transform.SetLocalRotation(projectile, newRot.ToAngle());

        PlayAudioAndPopup(reflector.Comp, user);

        if (Resolve(projectile, ref projectile.Comp, false))
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected {ToPrettyString(projectile)} from {ToPrettyString(projectile.Comp.Weapon)} shot by {projectile.Comp.Shooter}");

            projectile.Comp.Shooter = user;
            projectile.Comp.Weapon = user;
            Dirty(projectile, projectile.Comp);
        }
        else
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected {ToPrettyString(projectile)}");
        }

        return true;
    }
    private bool TryReflectHitscan(
        Entity<ReflectComponent> reflector,
        EntityUid user,
        EntityUid? shooter,
        EntityUid shotSource,
        Vector2 direction,
        ReflectType hitscanReflectType,
        [NotNullWhen(true)] out Vector2? newDirection)
    {
        if ((reflector.Comp.Reflects & hitscanReflectType) == 0x0 ||
            !_toggle.IsActivated(reflector.Owner) ||
            !_random.Prob(reflector.Comp.ReflectProb))
        {
            newDirection = null;
            return false;
        }

        PlayAudioAndPopup(reflector.Comp, user);

        var spread = _random.NextAngle(-reflector.Comp.Spread / 2, reflector.Comp.Spread / 2);
        newDirection = -spread.RotateVec(direction);

        if (shooter != null)
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)} shot by {ToPrettyString(shooter.Value)}");
        else
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)}");

        return true;
    }

    private void PlayAudioAndPopup(ReflectComponent reflect, EntityUid user)
    {
        // Can probably be changed for prediction
        if (_netManager.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), user);
            _audio.PlayPvs(reflect.SoundOnReflect, user);
        }
    }

    private void OnReflectEquipped(Entity<ReflectComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.InRightPlace = (ent.Comp.SlotFlags & args.SlotFlags) == args.SlotFlags;
        Dirty(ent);
    }

    private void OnReflectUnequipped(Entity<ReflectComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.InRightPlace = false;
        Dirty(ent);
    }

    private void OnReflectHandEquipped(Entity<ReflectComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.InRightPlace = ent.Comp.ReflectingInHands;
        Dirty(ent);
    }

    private void OnReflectHandUnequipped(Entity<ReflectComponent> ent, ref GotUnequippedHandEvent args)
    {
        ent.Comp.InRightPlace = false;
        Dirty(ent);
    }
}
