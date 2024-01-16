using Content.Server.Kitchen.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<SharpComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);
    }

    private void OnGetInteractionVerbsMelee(
        EntityUid uid, 
        SharpComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;
        
        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartExecutionDoafterMelee(args.Using!.Value, args.Target, args.User);
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

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartExecutionDoafterGun(args.Using!.Value, args.Target, args.User);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool TryStartExecutionDoafterCommonChecks(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out var damage))
            return false;
        
        // You're not allowed to execute dead people (no fun allowed)
        if (TryComp<MobStateComponent>(victim, out var mobState) && !_mobStateSystem.IsDead(victim, mobState))
            return false;

        // All checks passed
        return true;
    }
    
    private void TryStartExecutionDoafterMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!TryStartExecutionDoafterCommonChecks(weapon, victim, user))
            return;
        
        // We must be able to actually fire the gun and have it do damage
        if (!TryComp<GunComponent>(weapon, out var gun))
            return;
    }
    
    private void TryStartExecutionDoafterGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!TryStartExecutionDoafterCommonChecks(weapon, victim, user))
            return;
        
        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return;
    }
}