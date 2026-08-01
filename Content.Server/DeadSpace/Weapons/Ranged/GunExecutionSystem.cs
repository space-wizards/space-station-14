// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Content.Server.Chat;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Weapons.Ranged;
using Content.Shared.DoAfter;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Weapons.Ranged;

public sealed class GunExecutionSystem : EntitySystem
{
    private static readonly TimeSpan ExecutionDuration = TimeSpan.FromSeconds(5);
    private const float CourageFailureChance = 0.25f;
    private static readonly FixedPoint2 MinimumExecutionDamage = FixedPoint2.New(2);
    private static readonly FixedPoint2 TriggerPayloadMinimumExecutionDamage = FixedPoint2.New(7);
    private const float MinimumExecutionExplosionIntensity = 1f;

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SuicideSystem _suicide = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GunComponent, GunExecutionDoAfterEvent>(OnExecutionDoAfter);
    }

    private void OnGetVerbs(Entity<GunComponent> gun, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null ||
            args.Using != gun.Owner ||
            !args.CanAccess ||
            !HasComp<DamageableComponent>(args.Target) ||
            !HasComp<MobStateComponent>(args.Target) ||
            !HasLethalExecutionShot(gun))
        {
            return;
        }

        var user = args.User;
        var target = args.Target;
        var self = user == target;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => TryStartExecution(gun, user, target),
            IconEntity = GetNetEntity(gun.Owner),
            Impact = LogImpact.Extreme,
            Text = Loc.GetString(self ? "gun-execution-verb-suicide" : "gun-execution-verb-execute"),
            Message = Loc.GetString(self ? "gun-execution-verb-suicide-message" : "gun-execution-verb-execute-message"),
        });
    }

    private void TryStartExecution(Entity<GunComponent> gun, EntityUid user, EntityUid target)
    {
        if (!CanExecute(gun, user, target, out var failure))
        {
            ShowFailure(user, failure);
            return;
        }

        if (user == target && _random.Prob(CourageFailureChance))
        {
            EnsureComp<GunSuicideBlockedComponent>(user);
            _popup.PopupClient(
                Loc.GetString("gun-execution-popup-courage-failed"),
                user,
                user,
                PopupType.LargeCaution);
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            ExecutionDuration,
            new GunExecutionDoAfterEvent(),
            gun.Owner,
            target: target,
            used: gun.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ShowFailure(user, "gun-execution-error-start-failed");
            return;
        }

        var message = Loc.GetString(
            "gun-execution-popup-start",
            ("weapon", Identity.Entity(gun.Owner, EntityManager)),
            ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(message, user, Filter.Pvs(user), true, PopupType.LargeCaution);
    }

    private void OnExecutionDoAfter(Entity<GunComponent> gun, ref GunExecutionDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            ShowFailure(args.User, "gun-execution-error-interrupted");
            return;
        }

        if (args.Target == null || args.Used != gun.Owner)
        {
            ShowFailure(args.User, "gun-execution-error-invalid");
            return;
        }

        var user = args.User;
        var target = args.Target.Value;
        if (!CanExecute(gun, user, target, out var failure))
        {
            ShowFailure(user, failure);
            return;
        }

        var self = user == target;
        var targetCoordinates = self
            ? Transform(user).Coordinates.Offset(Vector2.UnitY)
            : Transform(target).Coordinates;

        if (!self)
            EnsureComp<GunExecutionShotComponent>(gun);

        bool fired;
        try
        {
            fired = _gun.AttemptShoot(user, gun, targetCoordinates, target);
        }
        finally
        {
            RemComp<GunExecutionShotComponent>(gun);
            _gun.StopExecutionShooting(gun);
        }

        if (!fired)
        {
            ShowFailure(user, "gun-execution-error-shot-failed");
            return;
        }

        args.Handled = true;
        if (self && !_suicide.Suicide(user, suppressDefaultPopup: true))
            ShowFailure(user, "gun-execution-error-suicide-failed");
    }

    private bool CanExecute(Entity<GunComponent> gun, EntityUid user, EntityUid target, out string failure)
    {
        failure = "gun-execution-error-invalid";

        if (Deleted(user) || Deleted(target) || Deleted(gun.Owner))
            return false;

        if (_hands.GetActiveItem(user) != gun.Owner)
        {
            failure = "gun-execution-error-active-hand";
            return false;
        }

        if (!TryComp<MobStateComponent>(user, out var userState) || _mobState.IsDead(user, userState))
        {
            failure = "gun-execution-error-user-dead";
            return false;
        }

        if (!HasComp<DamageableComponent>(target) ||
            !TryComp<MobStateComponent>(target, out var targetState))
        {
            failure = "gun-execution-error-invalid-target";
            return false;
        }

        if (_mobState.IsDead(target, targetState))
        {
            failure = "gun-execution-error-target-dead";
            return false;
        }

        if (user == target && HasComp<GunSuicideBlockedComponent>(user))
        {
            failure = "gun-execution-error-courage-blocked";
            return false;
        }

        if (HasComp<GunRequiresWieldComponent>(gun) &&
            (!TryComp<WieldableComponent>(gun, out var wieldable) || !wieldable.Wielded))
        {
            failure = "gun-execution-error-not-wielded";
            return false;
        }

        if (!_actionBlocker.CanAttack(user, target))
        {
            failure = "gun-execution-error-cannot-attack";
            return false;
        }

        if (!_interaction.InRangeUnobstructed(user, target))
        {
            failure = "gun-execution-error-range";
            return false;
        }

        if (!TryGetLethalExecutionShot(gun, out failure))
        {
            return false;
        }

        if (!_gun.CanShoot(gun.Comp))
        {
            failure = "gun-execution-error-not-ready";
            return false;
        }

        return true;
    }

    private bool HasLethalExecutionShot(Entity<GunComponent> gun)
    {
        return TryGetLethalExecutionShot(gun, out _);
    }

    private bool TryGetLethalExecutionShot(Entity<GunComponent> gun, out string failure)
    {
        failure = "gun-execution-error-nonlethal";

        if (HasComp<PacifismAllowedGunComponent>(gun))
            return false;

        if (TryComp<BatteryWeaponFireModesComponent>(gun, out var fireModes))
        {
            if (fireModes.CurrentFireMode < 0 ||
                fireModes.CurrentFireMode >= fireModes.FireModes.Count ||
                fireModes.FireModes[fireModes.CurrentFireMode].PacifismAllowedMode)
            {
                return false;
            }
        }

        var ammo = new GetAmmoCountEvent();
        RaiseLocalEvent(gun, ref ammo);
        if (ammo.Count <= 0 ||
            !TryResolveExecutionProjectile(
                ref ammo.NextAmmoEntity,
                ref ammo.NextAmmoPrototype))
        {
            failure = "gun-execution-error-empty";
            return false;
        }

        if (ammo.NextAmmoEntity is { } entity)
            return IsLethalExecutionProjectile(entity);

        return ammo.NextAmmoPrototype is { } prototype &&
               IsLethalExecutionProjectile(prototype);
    }

    private bool TryResolveExecutionProjectile(ref EntityUid? entity, ref EntProtoId? prototype)
    {
        const int depthLimit = 8;
        for (var depth = 0; depth < depthLimit; depth++)
        {
            if (entity is { } ammoEntity)
            {
                if (!TryComp<CartridgeAmmoComponent>(ammoEntity, out var cartridge))
                    return true;

                if (cartridge.Spent)
                    return false;

                entity = null;
                prototype = cartridge.Prototype;
                continue;
            }

            if (prototype is not { } ammoPrototype ||
                !_prototype.TryIndex(ammoPrototype, out var entityPrototype))
            {
                return false;
            }

            if (!entityPrototype.TryGetComponent<CartridgeAmmoComponent>(out var prototypeCartridge, Factory))
                return true;

            if (prototypeCartridge.Spent)
                return false;

            prototype = prototypeCartridge.Prototype;
        }

        return false;
    }

    private bool IsLethalExecutionProjectile(EntityUid projectile)
    {
        if (HasComp<StaminaDamageOnCollideComponent>(projectile) ||
            HasComp<HitscanStaminaDamageComponent>(projectile))
        {
            return false;
        }

        var triggerPayload = HasComp<TriggerOnCollideComponent>(projectile);
        if (TryComp<ProjectileComponent>(projectile, out var projectileComponent) &&
            HasEnoughExecutionDamage(projectileComponent.Damage, triggerPayload))
        {
            return true;
        }

        if (TryComp<HitscanBasicDamageComponent>(projectile, out var hitscan) &&
            HasEnoughExecutionDamage(hitscan.Damage, triggerPayload))
        {
            return true;
        }

        return TryComp<ExplosiveComponent>(projectile, out var explosive) &&
               HasEnoughExecutionExplosion(explosive);
    }

    private bool IsLethalExecutionProjectile(EntProtoId projectile)
    {
        if (!_prototype.TryIndex(projectile, out var prototype))
            return false;

        if (prototype.TryGetComponent<StaminaDamageOnCollideComponent>(out _, Factory) ||
            prototype.TryGetComponent<HitscanStaminaDamageComponent>(out _, Factory))
        {
            return false;
        }

        var triggerPayload = prototype.TryGetComponent<TriggerOnCollideComponent>(out _, Factory);
        if (prototype.TryGetComponent<ProjectileComponent>(out var projectileComponent, Factory) &&
            HasEnoughExecutionDamage(projectileComponent.Damage, triggerPayload))
        {
            return true;
        }

        if (prototype.TryGetComponent<HitscanBasicDamageComponent>(out var hitscan, Factory) &&
            HasEnoughExecutionDamage(hitscan.Damage, triggerPayload))
        {
            return true;
        }

        return prototype.TryGetComponent<ExplosiveComponent>(out var explosive, Factory) &&
               HasEnoughExecutionExplosion(explosive);
    }

    private static bool HasEnoughExecutionDamage(
        Content.Shared.Damage.DamageSpecifier damage,
        bool triggerPayload)
    {
        var total = FixedPoint2.Zero;
        foreach (var (type, value) in damage.DamageDict)
        {
            if (type != "Structural" && value > FixedPoint2.Zero)
                total += value;
        }

        var minimumDamage = triggerPayload
            ? TriggerPayloadMinimumExecutionDamage
            : MinimumExecutionDamage;
        return total > minimumDamage;
    }

    private static bool HasEnoughExecutionExplosion(ExplosiveComponent explosive)
    {
        return explosive.TotalIntensity >= MinimumExecutionExplosionIntensity;
    }

    private void ShowFailure(EntityUid user, string failure)
    {
        if (Deleted(user))
            return;

        _popup.PopupClient(Loc.GetString(failure), user, user, PopupType.MediumCaution);
    }
}
