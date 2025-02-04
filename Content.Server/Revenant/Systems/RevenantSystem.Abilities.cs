using Content.Server.Abilities;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Revenant.Components;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant;


namespace Content.Server.Revenant.Systems;

public sealed partial class RevenantSystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<RevenantComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulEvent>(OnSoulSearch);
        SubscribeLocalEvent<RevenantComponent, HarvestEvent>(OnHarvest);

        // We need to catch these before the AbilitySystem so we can handle it
        SubscribeLocalEvent<RevenantComponent, RevenantDefileActionEvent>(HandleRevenantAction, before: [typeof(AbilitySystem)]);
        SubscribeLocalEvent<RevenantComponent, RevenantOverloadLightsActionEvent>(HandleRevenantAction, before: [typeof(AbilitySystem)]);
        SubscribeLocalEvent<RevenantComponent, RevenantMalfunctionActionEvent>(HandleRevenantAction, before: [typeof(AbilitySystem)]);
        SubscribeLocalEvent<RevenantComponent, RevenantColdSnapActionEvent>(HandleRevenantAction, before: [typeof(AbilitySystem)]);
        SubscribeLocalEvent<RevenantComponent, RevenantEnergyDrainActionEvent>(HandleRevenantAction, before: [typeof(AbilitySystem)]);
    }

    private void HandleRevenantAction<T>(Entity<RevenantComponent> ent, ref T args) where T : InstantActionEvent
    {
        if (args.Handled)
            return;

        // If we can't find the component, assume it was intentionally removed, and don't block the action
        if (!TryComp<RevenantActionComponent>(args.Action, out var revenantAction))
            return;

        // If we can't use the ability, mark it as handled so that the AbilitySystem doesn't perform it
        if (!TryUseAbility(ent, revenantAction))
            args.Handled = true;
    }

    private void OnInteract(Entity<RevenantComponent> ent, ref  UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;

        var target = args.Target;

        if (HasComp<PoweredLightComponent>(target))
        {
            args.Handled = _ghost.DoGhostBooEvent(target);
            return;
        }

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) || HasComp<RevenantComponent>(target))
            return;

        if (!TryComp<EssenceComponent>(target, out var essence) || !essence.SearchComplete)
        {
            EnsureComp<EssenceComponent>(target);
            BeginSoulSearchDoAfter(ent, target);
        }
        else
        {
            BeginHarvestDoAfter(ent, (target, essence));
        }

        args.Handled = true;
    }

    private void BeginSoulSearchDoAfter(Entity<RevenantComponent> ent, EntityUid target)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.SoulSearchDuration,
            new SoulEvent(),
            ent,
            target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-searching", ("target", target)), ent, ent, PopupType.Medium);
    }

    private void OnSoulSearch(Entity<RevenantComponent> ent, ref SoulEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;

        essence.SearchComplete = true;

        var message = essence.EssenceAmount switch
        {
            <= 45 => "revenant-soul-yield-low",
            >= 90 => "revenant-soul-yield-high",
            _ => "revenant-soul-yield-average",
        };

        _popup.PopupEntity(Loc.GetString(message, ("target", args.Args.Target)), args.Args.Target.Value, ent, PopupType.Medium);

        args.Handled = true;
    }

    private void BeginHarvestDoAfter(Entity<RevenantComponent> ent, Entity<EssenceComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (target.Comp.Harvested)
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-harvested"), target, ent, PopupType.SmallCaution);
            return;
        }

        if (TryComp<MobStateComponent>(target, out var mobstate)
            && mobstate.CurrentState == MobState.Alive &&
            !HasComp<SleepingComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-too-powerful"), target, ent);
            return;
        }

        if (_physics.GetEntitiesIntersectingBody(ent, (int) CollisionGroup.Impassable).Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("revenant-in-solid"), ent, ent);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.HarvestStunTime, new HarvestEvent(), ent, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        Appearance.SetData(ent, RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target,
            PopupType.Large);

        _statusEffects.TryAddStatusEffect<CorporealComponent>(ent, "Corporeal", ent.Comp.HarvestCorporealTime, false);
        _stun.TryStun(ent, ent.Comp.HarvestStunTime, false);
    }

    private void OnHarvest(Entity<RevenantComponent> ent, ref HarvestEvent args)
    {
        if (args.Cancelled)
        {
            Appearance.SetData(ent, RevenantVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        Appearance.SetData(ent, RevenantVisuals.Harvesting, false);

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", args.Args.Target)),
            args.Args.Target.Value,
            PopupType.LargeCaution);

        essence.Harvested = true;
        TryChangeEssenceAmount(ent.AsNullable(), essence.EssenceAmount);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {ent.Comp.StolenEssenceCurrencyPrototype, essence.EssenceAmount} },
            ent);

        if (!HasComp<MobStateComponent>(args.Args.Target))
            return;

        if (_mobState.IsAlive(args.Args.Target.Value) || _mobState.IsCritical(args.Args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), ent, ent);
            ent.Comp.EssenceRegenCap += ent.Comp.MaxEssenceUpgradeAmount;
            Dirty(ent);
        }

        //KILL THEMMMM

        if (!_mobThreshold.TryGetThresholdForState(args.Args.Target.Value, MobState.Dead, out var damage))
            return;
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cold", damage.Value);
        _damage.TryChangeDamage(args.Args.Target, dspec, true, origin: ent);

        args.Handled = true;
    }
}
