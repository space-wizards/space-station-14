using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Content.Shared.Tag;
using Content.Shared.Storage.Components;
using Content.Server.Ghost;
using Content.Server.Lightning;
using Content.Server.Silicons.Laws;
using Robust.Shared.Physics;
using Content.Shared.Throwing;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mindshield.Components;
using Content.Shared.Silicons.Laws.Components;
using System.Linq;
using System.Numerics;
using Content.Server.Revenant.Components;
using Content.Shared.Physics;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;
using Robust.Shared.Map.Components;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Content.Shared.Corvax.TTS;
using Content.Shared.Ghost;
using Robust.Shared.Containers;

using Content.Shared.DeadSpace.Languages.Components;
using Content.Shared.Beam.Components;
using Content.Shared.Damage.Components;
using Robust.Shared.Audio.Systems; //DS14
using Robust.Shared.Player; //DS14
using Content.Shared.Actions; //DS14

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EmagSystem _emagSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly IonStormSystem _ionStorm = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; //DS14
    [Dependency] private readonly SharedActionsSystem _actions = default!; //DS14

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<RevenantComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulEvent>(OnSoulSearch);
        SubscribeLocalEvent<RevenantComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<RevenantComponent, RevenantDefileActionEvent>(OnDefileAction);
        SubscribeLocalEvent<RevenantComponent, RevenantOverloadLightsActionEvent>(OnOverloadLightsAction);
        SubscribeLocalEvent<RevenantComponent, RevenantBlightActionEvent>(OnBlightAction);
        SubscribeLocalEvent<RevenantComponent, RevenantMalfunctionActionEvent>(OnMalfunctionAction);
        //DS14-start
        SubscribeLocalEvent<RevenantComponent, RevenantSleepActionEvent>(OnSleepAction);
        SubscribeLocalEvent<RevenantComponent, RevenantMindCaptureActionEvent>(OnMindCaptureAction);
        SubscribeLocalEvent<RevenantComponent, RevenantBeamFireActionEvent>(OnBeamFireAction);
        SubscribeLocalEvent<RevenantComponent, RevenantScreamActionEvent>(OnScreamAction); //DS14
        SubscribeLocalEvent<RevenantComponent, RevenantHackActionEvent>(OnHackAction); //DS14
        //DS14-end
    }

    private void OnInteract(EntityUid uid, RevenantComponent component, UserActivateInWorldEvent args)
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

        args.Handled = true;
    }

    private void BeginSoulSearchDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, revenant.SoulSearchDuration, new SoulEvent(), uid, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-searching", ("target", target)), uid, uid, PopupType.Medium);
    }

    private void OnSoulSearch(EntityUid uid, RevenantComponent component, SoulEvent args)
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

    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
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

        if (_physics.GetEntitiesIntersectingBody(uid, (int)CollisionGroup.Impassable).Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, revenant.HarvestDebuffs.X, new HarvestEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target, PopupType.Large);

        TryUseAbility(uid, revenant, 0, revenant.HarvestDebuffs);
    }

    private void OnHarvest(EntityUid uid, RevenantComponent component, HarvestEvent args)
    {
        if (args.Cancelled)
        {
            _appearance.SetData(uid, RevenantVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);

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

        var protoId = MetaData(args.Args.Target.Value).EntityPrototype?.ID;

        DamageSpecifier dspec = new();
        dspec.DamageDict.Add(protoId == "MobIPC" ? "Heat" : "Cold", damage.Value);
        _damage.ChangeDamage(args.Args.Target.Value, dspec, true, origin: uid);

        args.Handled = true;
    }

    private void OnDefileAction(EntityUid uid, RevenantComponent component, RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.DefileCost, component.DefileDebuffs))
            return;

        args.Handled = true;

        //var coords = Transform(uid).Coordinates;
        //var gridId = coords.GetGridUid(EntityManager);
        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;
        var tiles = _mapSystem.GetTilesIntersecting(
            xform.GridUid.Value,
            map,
            Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
            new Vector2(component.DefileRadius * 2, component.DefileRadius)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < component.DefileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(uid, component.DefileRadius, LookupFlags.Approximate | LookupFlags.Static);
        var tags = GetEntityQuery<TagComponent>();
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in lookup)
        {
            //break windows
            if (tags.HasComponent(ent) && _tag.HasTag(ent, WindowTag))
            {
                //hardcoded damage specifiers til i die.
                var dspec = new DamageSpecifier();
                dspec.DamageDict.Add("Structural", 60);
                _damage.TryChangeDamage(ent, dspec, origin: uid);
            }

            if (!_random.Prob(component.DefileEffectChance))
                continue;

            //randomly opens some lockers and such.
            if (entityStorage.TryGetComponent(ent, out var entstorecomp))
                _entityStorage.OpenStorage(ent, entstorecomp);

            //chucks shit
            if (items.HasComponent(ent) &&
                TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

            //flicker lights
            if (lights.HasComponent(ent))
                _ghost.DoGhostBooEvent(ent);
        }
    }

    private void OnOverloadLightsAction(EntityUid uid, RevenantComponent component, RevenantOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.OverloadCost, component.OverloadDebuffs))
            return;

        args.Handled = true;

        var xform = Transform(uid);
        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var mobState = GetEntityQuery<MobStateComponent>();
        var lookup = _lookup.GetEntitiesInRange(uid, component.OverloadRadius);
        //TODO: feels like this might be a sin and a half
        foreach (var ent in lookup)
        {
            if (!mobState.HasComponent(ent) || !_mobState.IsAlive(ent))
                continue;

            var nearbyLights = _lookup.GetEntitiesInRange(ent, component.OverloadZapRadius)
                .Where(e => poweredLights.HasComponent(e) && !HasComp<RevenantOverloadedLightsComponent>(e) &&
                            _interact.InRangeUnobstructed(e, uid, -1)).ToArray();

            if (!nearbyLights.Any())
                continue;

            //get the closest light
            var allLight = nearbyLights.OrderBy(e =>
                Transform(e).Coordinates.TryDistance(EntityManager, xform.Coordinates, out var dist) ? component.OverloadZapRadius : dist);
            var comp = EnsureComp<RevenantOverloadedLightsComponent>(allLight.First());
            comp.Target = ent; //who they gon fire at?
        }
    }

    private void OnBlightAction(EntityUid uid, RevenantComponent component, RevenantBlightActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.BlightCost, component.BlightDebuffs))
            return;

        args.Handled = true;
        // TODO: When disease refactor is in.
    }

    private void OnMalfunctionAction(EntityUid uid, RevenantComponent component, RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.MalfunctionCost, component.MalfunctionDebuffs))
            return;

        args.Handled = true;

        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.MalfunctionRadius))
        {
            if (_whitelistSystem.IsWhitelistFail(component.MalfunctionWhitelist, ent) ||
                _whitelistSystem.IsWhitelistPass(component.MalfunctionBlacklist, ent))
                continue;

            //DS14-start
            if (TryComp<IonStormTargetComponent>(ent, out var target) && TryComp<SiliconLawBoundComponent>(ent, out var lawBound))
                _ionStorm.IonStormTarget((ent, lawBound, target));
            //DS14-end

            _emagSystem.TryEmagEffect(uid, uid, ent);
        }
    }

    //DS14-start
    private void OnSleepAction(EntityUid uid, RevenantComponent component, RevenantSleepActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<MindShieldComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-sleep-too-powerful"), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.SleepCost, component.SleepDebuffs))
            return;

        args.Handled = true;

        EnsureComp<RevenantForcedSleepComponent>(args.Target);
    }

    private void OnMindCaptureAction(EntityUid uid, RevenantComponent component, RevenantMindCaptureActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<MindCaptureDefenceComponent>(args.Target))
            return;

        if (HasComp<CorporealComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("revenant-mind-capture-corporeal"), uid);
            return;
        }

        if (!HasComp<MobStateComponent>(args.Target) || !_mobState.IsDead(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-mind-capture-is-dead"), uid);
            return;
        }

        if (!TryComp<DamageableComponent>(args.Target, out var damageable)
        || !_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Critical, out var crit)
        || !_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Dead, out var dead)
        || damageable.TotalDamage > crit.Value * args.ThresholdModifier)
        {
            _popup.PopupEntity(Loc.GetString("revenant-mind-capture-many-damage"), uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.MindCaptureCost, component.MindCaptureDebuffs))
            return;

        if (!_mind.TryGetMind(args.Performer, out var perMind, out var _))
            return;

        args.Handled = true;

        // Component on target, don`t confuse with revenant comp.
        var comp = new RevenantMindCapturedComponent(uid, dead.Value, crit.Value);
        AddComp(args.Target, comp);

        _mobThresholdSystem.SetMobStateThreshold(args.Target, dead.Value * args.ThresholdModifier, MobState.Dead);
        _mobThresholdSystem.SetMobStateThreshold(args.Target, crit.Value * args.ThresholdModifier, MobState.Critical);

        //DS-14 Start
        if (TryComp<DamageableComponent>(args.Target, out damageable))
        {
            var healSpec = new DamageSpecifier();

            foreach (var (type, _) in damageable.Damage.DamageDict)
            {
                healSpec.DamageDict.Add(type, -damageable.Damage.DamageDict[type]);
            }

            _damage.TryChangeDamage(args.Target, healSpec, ignoreResistances: true, origin: uid);
        }
        //DS-14 End
        _mobState.ChangeMobState(args.Target, MobState.Alive);

        if (TryComp<LanguageComponent>(args.Target, out var targetLanguage) && TryComp<LanguageComponent>(uid, out var revLanguage))
        {
            comp.ReturnCantSpeakLanguages = targetLanguage.CantSpeakLanguages;
            targetLanguage.CantSpeakLanguages = revLanguage.CantSpeakLanguages;

            comp.ReturnKnownLanguages = targetLanguage.KnownLanguages;
            targetLanguage.KnownLanguages = revLanguage.KnownLanguages;
        }

        if (TryComp<TTSComponent>(args.Target, out var targetTTS) && TryComp<TTSComponent>(uid, out var revTTS))
        {
            string oldTTSProto = "";

            if (!string.IsNullOrEmpty(targetTTS.VoicePrototypeId))
                oldTTSProto = targetTTS.VoicePrototypeId;

            targetTTS.VoicePrototypeId = revTTS.VoicePrototypeId;

            comp.ReturnTTSPrototype = oldTTSProto;
        }

        if (_mind.TryGetMind(args.Target, out var tarMindID, out var tarMind))
        {
            if (TryComp<GhostComponent>(tarMind.VisitingEntity, out var tarGhostComp))
            {
                comp.TargetUid = tarMind.VisitingEntity.Value;
                _ghost.SetCanReturnToBody((tarMind.VisitingEntity.Value, tarGhostComp), false);
            }
            else
            {
                EntityUid? ghost = _ghost.SpawnGhost((tarMindID, tarMind), args.Target, true);

                if (TryComp<GhostComponent>(ghost, out var tarGhostCompEnsured))
                {
                    comp.TargetUid = ghost.Value;
                    _ghost.SetCanReturnToBody((ghost.Value, tarGhostCompEnsured), false);
                }
            }
        }

        comp.RevenantContainer = _container.EnsureContainer<Container>(args.Target, component.Container);
        _container.Insert(uid, comp.RevenantContainer);
        _mind.Visit(perMind, args.Target);
        if (TryComp<RevenantComponent>(uid, out var revenantComp))
        {
            _actions.AddAction(args.Target, ref revenantComp.HackActionEntity, revenantComp.HackAction, uid);
        }
    }

    private void OnBeamFireAction(EntityUid uid, RevenantComponent component, RevenantBeamFireActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.BeamFireCost, component.BeamFireDebuffs))
            return;

        args.Handled = true;

        if (!HasComp<MobStateComponent>(args.Target) || !_mobState.IsAlive(args.Target))
            return;

        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;

        var tiles = _mapSystem.GetTilesIntersecting(
            xform.GridUid.Value,
            map,
            Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
            new Vector2(component.DefileRadius * 2, component.DefileRadius)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < component.DefileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }

        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var lookup = _lookup.GetEntitiesInRange(uid, component.DefileRadius, LookupFlags.Approximate | LookupFlags.Static);

        foreach (var ent in lookup)
        {
            if (poweredLights.HasComponent(ent))
                _ghost.DoGhostBooEvent(ent);
        }

        _lightning.ShootLightning(uid, args.Target, component.BeamEntityId);
    }

    private void OnScreamAction(EntityUid uid, RevenantComponent component, RevenantScreamActionEvent args)
    {
        if (args.Handled)
            return;
        if (!TryUseAbility(uid, component, component.ScreamCost, component.ScreamDebuffs))
            return;
        args.Handled = true;

        var coords = Transform(uid).Coordinates;
        _audio.PlayStatic(component.ScreamSounds, Filter.Pvs(uid), coords, true);
    }

    private void OnHackAction(EntityUid uid, RevenantComponent component, RevenantHackActionEvent args)
    {
        if (args.Handled)
            return;
        if (!CanUseAbility(uid, component, component.HackCost))
            return;

        var target = args.Target;
        var effectApplied = _emagSystem.TryEmagEffect(uid, uid, target);
        if (TryComp<EntityStorageComponent>(target, out var storage))
        {
            _entityStorage.OpenStorage(target, storage);
            effectApplied = true;
        }

        if (!effectApplied)
            return;

        ApplyAbilityCostAndDebuffs(uid, component, component.HackCost, component.HackDebuffs);
        args.Handled = true;
    }
    //DS14-end
}
