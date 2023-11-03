using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Necromant;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Content.Server.Storage.Components;
using Content.Server.Light.Components;
using Content.Server.Ghost;

using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Bed.Sleep;
using System.Linq;
using System.Numerics;
using Content.Server.Maps;
using Content.Server.Revenant.Components;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Necromant.Components;

using Robust.Shared.Utility;

namespace Content.Server.Necromant.EntitySystems;

public sealed partial class NecromantSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<NecromantComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<NecromantComponent, SoulEvent>(OnSoulSearch);
        SubscribeLocalEvent<NecromantComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<NecromantComponent, NecromantRaiseArmyActionEvent>(OnRaiseArmy);
        SubscribeLocalEvent<NecromantComponent, NecromantRaiseInfectorActionEvent>(OnRaiseInfector);
        SubscribeLocalEvent<NecromantComponent, NecromantRaiseTwitcherActionEvent>(OnRaiseTwitcher);
        SubscribeLocalEvent<NecromantComponent, NecromantRaiseDivaderActionEvent>(OnRaiseDivader);
        SubscribeLocalEvent<NecromantComponent, NecromantRaisePregnantActionEvent>(OnRaisePregnant);
        SubscribeLocalEvent<NecromantComponent, NecromantRaiseBruteActionEvent>(OnRaiseBrute);

    }

    private void OnInteract(EntityUid uid, NecromantComponent component, InteractNoHandEvent args)
    {
        if (args.Target == args.User || args.Target == null)
            return;
        var target = args.Target.Value;

        if (HasComp<PoweredLightComponent>(target))
        {
            args.Handled = _ghost.DoGhostBooEvent(target);
            return;
        }

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) || HasComp<NecromantComponent>(target))
            return;

        args.Handled = true;
        if (!TryComp<EssenceComponent>(target, out var essence) || !essence.SearchComplete)
        {
            EnsureComp<EssenceComponent>(target);
            BeginSoulSearchDoAfter(uid, target, component);
        }
        else
        {
            BeginHarvestDoAfter(uid, target, component, essence);
        }
    }

    private void BeginSoulSearchDoAfter(EntityUid uid, EntityUid target, NecromantComponent necromant)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, necromant.SoulSearchDuration, new SoulEvent(), uid, target: target)
        {
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-searching", ("target", target)), uid, uid, PopupType.Medium);
    }

    private void OnSoulSearch(EntityUid uid, NecromantComponent component, SoulEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;
        essence.SearchComplete = true;

        string message;
        switch (essence.EssenceAmount)
        {
            case <= 45:
                message = "revenant-soul-yield-low";
                break;
            case >= 90:
                message = "revenant-soul-yield-high";
                break;
            default:
                message = "revenant-soul-yield-average";
                break;
        }
        _popup.PopupEntity(Loc.GetString(message, ("target", args.Args.Target)), args.Args.Target.Value, uid, PopupType.Medium);
        args.Handled = true;
    }

    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, NecromantComponent necromant, EssenceComponent essence)
    {
        if (essence.Harvested)
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-harvested"), target, uid, PopupType.SmallCaution);
            return;
        }

        if (TryComp<MobStateComponent>(target, out var mobstate) && mobstate.CurrentState == MobState.Alive && !HasComp<SleepingComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-too-powerful"), target, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, necromant.HarvestDebuffs.X, new HarvestEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, NecromantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target, PopupType.Large);

        TryUseAbility(uid, necromant, 0, necromant.HarvestDebuffs);
    }

    private void OnHarvest(EntityUid uid, NecromantComponent component, HarvestEvent args)
    {
        if (args.Cancelled)
        {
            _appearance.SetData(uid, NecromantVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        _appearance.SetData(uid, NecromantVisuals.Harvesting, false);

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", args.Args.Target)),
            args.Args.Target.Value, PopupType.LargeCaution);

        essence.Harvested = true;
        ChangeEssenceAmount(uid, essence.EssenceAmount, component);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {component.StolenEssenceCurrencyPrototype, essence.EssenceAmount} }, uid);

        if (!HasComp<MobStateComponent>(args.Args.Target))
            return;

        if (_mobState.IsAlive(args.Args.Target.Value) || _mobState.IsCritical(args.Args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), uid, uid);
            component.EssenceRegenCap += component.MaxEssenceUpgradeAmount;
        }

        //KILL THEMMMM

        if (!_mobThresholdSystem.TryGetThresholdForState(args.Args.Target.Value, MobState.Dead, out var damage))
            return;
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cold", damage.Value);
        _damage.TryChangeDamage(args.Args.Target, dspec, true, origin: uid);

        args.Handled = true;
    }

     private void OnRaiseArmy(EntityUid uid, NecromantComponent component, NecromantRaiseArmyActionEvent args)
    {
        if (args.Handled)
            return;


        if (!TryUseAbility(uid, component, component.ArmyCost, component.ArmyDebuffs))
            return;

        args.Handled = true;

        Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates);
    }

    private void OnRaiseInfector(EntityUid uid, NecromantComponent component, NecromantRaiseInfectorActionEvent args)
    {
        if (args.Handled)
            return;


        if (!TryUseAbility(uid, component, component.InfectorCost, component.ArmyDebuffs))
            return;

        args.Handled = true;

        Spawn(component.InfectorMobSpawnId, Transform(uid).Coordinates);
    }

    private void OnRaiseTwitcher(EntityUid uid, NecromantComponent component, NecromantRaiseTwitcherActionEvent args)
    {
        if (args.Handled)
            return;


        if (!TryUseAbility(uid, component, component.TwitcherCost, component.ArmyDebuffs))
            return;

        args.Handled = true;

        Spawn(component.TwitcherMobSpawnId, Transform(uid).Coordinates);
    }
    
    private void OnRaiseDivader(EntityUid uid, NecromantComponent component, NecromantRaiseDivaderActionEvent args)
    {
        if (args.Handled)
            return;


        if (!TryUseAbility(uid, component, component.DivaderCost, component.ArmyDebuffs))
            return;

        args.Handled = true;

        Spawn(component.DivaderMobSpawnId, Transform(uid).Coordinates);
    }

    private void OnRaisePregnant(EntityUid uid, NecromantComponent component, NecromantRaisePregnantActionEvent args)
    {
        if (args.Handled)
            return;


        if (!TryUseAbility(uid, component, component.PregnantCost, component.ArmyDebuffs))
            return;

        args.Handled = true;

        Spawn(component.PregnantMobSpawnId, Transform(uid).Coordinates);
    }

    private void OnRaiseBrute(EntityUid uid, NecromantComponent component, NecromantRaiseBruteActionEvent args)
    {
        if (args.Handled)
            return;


        if (!TryUseAbility(uid, component, component.BruteCost, component.ArmyDebuffs))
            return;

        args.Handled = true;

        Spawn(component.BruteMobSpawnId, Transform(uid).Coordinates);
    }

}
