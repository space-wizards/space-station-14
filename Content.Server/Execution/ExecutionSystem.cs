using Content.Server.Kitchen.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Player;

namespace Content.Server.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _meleeSystem = default!;

    private const float MeleeExecutionTimeModifier = 5.0f;
    private const float GunExecutionTime = 10.0f;
    private const float DamageModifier = 9.0f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<SharpComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);
        
        SubscribeLocalEvent<SharpComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);
    }

    private void OnGetInteractionVerbsMelee(
        EntityUid uid, 
        SharpComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;
        
        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithMelee(weapon, victim, victim))
            return;
        
        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartMeleeExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void OnGetInteractionVerbsGun(
        EntityUid uid, 
        GunComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithGun(weapon, victim, victim))
            return;
        
        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool CanExecuteWithAny(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out var damage))
            return false;
        
        // You're not allowed to execute dead people (no fun allowed)
        if (TryComp<MobStateComponent>(victim, out var mobState) && _mobStateSystem.IsDead(victim, mobState))
            return false;

        // You must be incapacitated to be executed
        if (_actionBlockerSystem.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user)) return false;
        
        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return false;

        return true;
    }
    
    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user)) return false;
        
        // We must be able to actually fire the gun and have it do damage
        if (!TryComp<GunComponent>(weapon, out var gun))
            return false;

        return true;
    }
    
    private void TryStartMeleeExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithMelee(weapon, victim, user))
            return;

        var executionTime = Comp<MeleeWeaponComponent>(weapon).AttackRate * MeleeExecutionTimeModifier;

        _popupSystem.PopupEntity(Loc.GetString(
                "execution-popup-melee-initial-internal", ("weapon", weapon), ("victim", victim)),
            user, Filter.Entities(user), true, PopupType.Medium);
        _popupSystem.PopupEntity(Loc.GetString(
                "execution-popup-melee-initial-external", ("weapon", weapon), ("victim", victim)),
            user, Filter.PvsExcept(user), true, PopupType.MediumCaution);
        
        var doAfter =
            new DoAfterArgs(EntityManager, user, executionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }
    
    private void TryStartGunExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithGun(weapon, victim, user))
            return;

        _popupSystem.PopupEntity(Loc.GetString(
                "execution-popup-gun-initial-internal", ("weapon", weapon), ("victim", victim)),
            user, Filter.Entities(user), true, PopupType.Medium);
        _popupSystem.PopupEntity(Loc.GetString(
                "execution-popup-gun-initial-external", ("weapon", weapon), ("victim", victim)),
            user, Filter.PvsExcept(user), true, PopupType.MediumCaution);
        
        var doAfter =
            new DoAfterArgs(EntityManager, user, GunExecutionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private bool OnDoafterChecks(EntityUid uid, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return false;
        
        if (!CanExecuteWithAny(args.Used.Value, args.Target.Value, uid))
            return false;
        
        // All checks passed
        return true;
    }

    private void OnDoafterMelee(EntityUid uid, SharpComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;
        
        var attacker = args.User;
        var weapon = args.Used!.Value;
        var victim = args.Target!.Value;

        if (!CanExecuteWithMelee(weapon, victim, attacker)) return;

        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return;
        
        _damageableSystem.TryChangeDamage(victim, melee.Damage * DamageModifier, true);
        _meleeSystem.PlayHitSound(victim, weapon, null, null, null);
        
        _popupSystem.PopupEntity(Loc.GetString(
                "execution-popup-melee-complete-internal", ("victim", victim)),
            attacker, Filter.Entities(attacker), true, PopupType.Medium);
        _popupSystem.PopupEntity(Loc.GetString(
                "execution-popup-melee-complete-external", ("attacker", attacker), ("victim", victim)),
            attacker, Filter.PvsExcept(attacker), true, PopupType.MediumCaution);
    }
    
    private void OnDoafterGun(EntityUid uid, GunComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;
        
        var attacker = args.User;
        var weapon = args.Used!.Value;
        var victim = args.Target!.Value;

        if (!CanExecuteWithGun(weapon, victim, attacker)) return;
    }
}