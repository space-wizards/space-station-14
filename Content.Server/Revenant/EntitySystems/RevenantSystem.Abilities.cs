using System.Linq;
using System.Numerics;
using Content.Server.Ghost;
using Content.Server.Revenant.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem
{
    [Dependency] private EmagSystem _emagSystem = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    [Dependency] private EntityQuery<ItemComponent> _itemQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<PoweredLightComponent> _poweredLightQuery;

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<RevenantComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulEvent>(OnSoulSearch);
        SubscribeLocalEvent<RevenantComponent, HarvestEvent>(OnHarvest);

    }

    private void OnInteract(EntityUid uid, RevenantComponent component, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;
        var target = args.Target;

        // Try to do something spooky first.
        if (_ghost.DoGhostBooEvent(target))
        {
            args.Handled = true;
            return;
        }

        if (!_mobStateQuery.HasComp(target) || !HasComp<HumanoidProfileComponent>(target) || HasComp<RevenantComponent>(target))
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

        _popup.PopupEntity(
            Loc.GetString("revenant-soul-searching", ("target", Identity.Entity(target, EntityManager))),
            uid,
            uid,
            PopupType.Medium);
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
        _popup.PopupEntity(
            Loc.GetString(message, ("target", Identity.Entity(args.Args.Target.Value, EntityManager))),
            args.Args.Target.Value,
            uid,
            PopupType.Medium);

        args.Handled = true;
    }

    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
    {
        if (essence.Harvested)
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-harvested"), target, uid, PopupType.SmallCaution);
            return;
        }

        if (_mobStateQuery.TryComp(target, out var mobstate) && mobstate.CurrentState == MobState.Alive && !HasComp<SleepingComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-too-powerful"), target, uid);
            return;
        }

        if (_physics.GetEntitiesIntersectingBody(uid, (int)CollisionGroup.Impassable).Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, revenant.HarvestStunDuration, new HarvestEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(
            Loc.GetString("revenant-soul-begin-harvest", ("target", Identity.Entity(target, EntityManager))),
            target,
            PopupType.Large);

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(revenant.HarvestCorporalDuration), false);
        _stun.TryAddStunDuration(uid, TimeSpan.FromSeconds(revenant.HarvestStunDuration));
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

        if (!TryComp<SatiationComponent>(uid, out var satiationComp))
            return;

        _popup.PopupEntity(
            Loc.GetString("revenant-soul-finish-harvest", ("target", Identity.Entity(args.Args.Target.Value, EntityManager))),
            args.Args.Target.Value,
            PopupType.LargeCaution);

        essence.Harvested = true;

        _satiation.ModifyValue((uid, satiationComp), component.EssenceSatiationPrototype, essence.EssenceAmount);

        _store.TryAddCurrency(new()
            { {component.StolenEssenceCurrencyPrototype, essence.EssenceAmount} }, uid);
        _store.UpdateUserInterface(uid, uid);

        if (!_mobStateQuery.HasComp(args.Args.Target))
            return;

        if (_mobState.IsAlive(args.Args.Target.Value) || _mobState.IsCritical(args.Args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), uid, uid);

            _satiation.ModifyOverrideThreshold((uid, satiationComp), component.EssenceSatiationPrototype, component.MaxRegenThreshold, component.MaxEssenceUpgradeAmount);
        }

        //KILL THEMMMM

        if (!_mobThresholdSystem.TryGetThresholdForState(args.Args.Target.Value, MobState.Dead, out var damage))
            return;

        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cold", damage.Value);
        _damage.ChangeDamage(args.Args.Target.Value, dspec, true, origin: uid);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDefileAction(Entity<RevenantDefileActionComponent> ent, ref RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _statusEffects.TryAddStatusEffect<CorporealComponent>(args.Performer, "Corporeal", TimeSpan.FromSeconds(ent.Comp.CorporealDuration), false);
        _stun.TryAddStunDuration(args.Performer, TimeSpan.FromSeconds(ent.Comp.StunDuration));

        var xform = Transform(args.Performer);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;

        var tiles = _mapSystem.GetTilesIntersecting(
            xform.GridUid.Value,
            map,
            Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
            new Vector2(ent.Comp.DefileRadius * 2, ent.Comp.DefileRadius)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < ent.Comp.DefileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(args.Performer, ent.Comp.DefileRadius, LookupFlags.Approximate | LookupFlags.Static);
        foreach (var entity in lookup)
        {
            //break windows
            if (_tag.HasTag(ent, WindowTag))
            {
                //hardcoded damage specifiers til i die.
                var dspec = new DamageSpecifier();
                dspec.DamageDict.Add("Structural", 60);
                _damage.TryChangeDamage(entity, dspec, origin: args.Performer);
            }

            if (!_random.Prob(ent.Comp.DefileEffectChance))
                continue;

            //randomly opens some lockers and such.
            _entityStorage.OpenStorage(entity, args.Performer);

            //chucks shit
            if (_itemQuery.HasComp(entity) &&
                TryComp<PhysicsComponent>(entity, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(entity, _random.NextAngle().ToWorldVec());

            //spooky stuff
            _ghost.DoGhostBooEvent(entity);
        }
    }

    [SubscribeLocalEvent]
    private void OnOverloadLightsAction(Entity<RevenantOverloadLightsActionComponent> ent, ref RevenantOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _statusEffects.TryAddStatusEffect<CorporealComponent>(args.Performer, "Corporeal", TimeSpan.FromSeconds(ent.Comp.CorporealDuration), false);
        _stun.TryAddStunDuration(args.Performer, TimeSpan.FromSeconds(ent.Comp.StunDuration));

        var xform = Transform(args.Performer);
        var lookup = _lookup.GetEntitiesInRange(args.Performer, ent.Comp.OverloadRadius);
        //TODO: feels like this might be a sin and a half
        foreach (var entity in lookup)
        {
            if (!_mobStateQuery.HasComp(entity) || !_mobState.IsAlive(entity))
                continue;

            var nearbyLights = _lookup.GetEntitiesInRange(entity, ent.Comp.OverloadZapRadius)
                .Where(e => _poweredLightQuery.HasComp(e) && !HasComp<RevenantOverloadedLightsComponent>(e) &&
                            _interact.InRangeUnobstructed(e, ent.Owner, -1)).ToArray();

            if (!nearbyLights.Any())
                continue;

            //get the closest light
            var allLight = nearbyLights.OrderBy(e =>
                Transform(e).Coordinates.TryDistance(EntityManager, xform.Coordinates, out var dist) ? ent.Comp.OverloadZapRadius : dist);
            var comp = EnsureComp<RevenantOverloadedLightsComponent>(allLight.First());
            comp.Target = entity; //who they gon fire at?
        }
    }

    [SubscribeLocalEvent]
    private void OnMalfunctionAction(Entity<RevenantMalfunctionActionComponent> ent, ref RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _statusEffects.TryAddStatusEffect<CorporealComponent>(args.Performer, "Corporeal", TimeSpan.FromSeconds(ent.Comp.CorporealDuration), false);
        _stun.TryAddStunDuration(args.Performer, TimeSpan.FromSeconds(ent.Comp.StunDuration));

        foreach (var entity in _lookup.GetEntitiesInRange(args.Performer, ent.Comp.MalfunctionRadius))
        {
            if (_whitelistSystem.IsWhitelistFail(ent.Comp.MalfunctionWhitelist, entity) ||
                _whitelistSystem.IsWhitelistPass(ent.Comp.MalfunctionBlacklist, entity))
                continue;

            _emagSystem.TryEmagEffect(args.Performer, args.Performer, entity);
        }
    }
}
