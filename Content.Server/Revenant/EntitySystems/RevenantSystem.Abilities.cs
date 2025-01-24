using System.Linq;
using System.Numerics;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Revenant.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using EssenceComponent = Content.Shared.Revenant.Components.EssenceComponent;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<RevenantComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulEvent>(OnSoulSearch);
        SubscribeLocalEvent<RevenantComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<RevenantComponent, RevenantDefileActionEvent>(OnDefileAction);
        SubscribeLocalEvent<RevenantComponent, RevenantOverloadLightsActionEvent>(OnOverloadLightsAction);
        SubscribeLocalEvent<RevenantComponent, RevenantMalfunctionActionEvent>(OnMalfunctionAction);
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

    private void OnDefileAction(Entity<RevenantComponent> ent, ref RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RevenantActionComponent>(args.Action, out var revenantAction))
            return;

        if (!TryComp<DefileActionComponent>(args.Action, out var defileAction))
            return;

        if (!TryUseAbility(ent, revenantAction))
            return;

        args.Handled = true;

        var xform = Transform(ent);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;

        var tiles = _map.GetTilesIntersecting(
            xform.GridUid.Value,
            map,
            Box2.CenteredAround(_xform.GetWorldPosition(xform),
            new Vector2(defileAction.DefileRadius * 2, defileAction.DefileRadius)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < defileAction.DefileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(ent, defileAction.DefileRadius, LookupFlags.Approximate | LookupFlags.Static);
        var tags = GetEntityQuery<TagComponent>();
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var foundEnt in lookup)
        {
            //break windows
            if (tags.HasComponent(foundEnt) && _tag.HasTag(foundEnt, "Window"))
            {
                var dspec = new DamageSpecifier();
                dspec.DamageDict.Add("Structural", 60);
                _damage.TryChangeDamage(foundEnt, dspec, origin: ent);
            }

            if (!_random.Prob(defileAction.DefileEffectChance))
                continue;

            //randomly opens some lockers and such.
            if (entityStorage.HasComponent(foundEnt))
                _entityStorage.OpenStorage(foundEnt);

            //chucks shit
            if (items.HasComponent(foundEnt) &&
                TryComp<PhysicsComponent>(foundEnt, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(foundEnt, _random.NextAngle().ToWorldVec());

            //flicker lights
            if (lights.HasComponent(foundEnt))
                _ghost.DoGhostBooEvent(foundEnt);
        }
    }

    private void OnOverloadLightsAction(Entity<RevenantComponent> ent, ref RevenantOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RevenantActionComponent>(args.Action, out var revenantAction))
            return;

        if (!TryComp<OverloadLightsActionComponent>(args.Action, out var overloadAction))
            return;

        if (!TryUseAbility(ent, revenantAction))
            return;

        args.Handled = true;

        var xform = Transform(ent);
        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var mobState = GetEntityQuery<MobStateComponent>();
        var lookup = _lookup.GetEntitiesInRange(ent, overloadAction.OverloadRadius);

        foreach (var foundEnt in lookup)
        {
            if (!mobState.HasComponent(foundEnt) || !_mobState.IsAlive(foundEnt))
                continue;

            var targetXform = Transform(foundEnt);

            // Get nearby lights
            var nearbyLights = new List<EntityUid>();
            var nearbyEntities = _lookup.GetEntitiesInRange(foundEnt, overloadAction.OverloadZapRadius);

            foreach (var nearbyEnt in nearbyEntities)
            {
                if (!poweredLights.HasComponent(nearbyEnt) ||
                    HasComp<RevenantOverloadedLightsComponent>(nearbyEnt))
                    continue;

                // Check if the light is unobstructed FROM THE TARGET
                var lightXform = Transform(nearbyEnt);
                if (!_interact.InRangeUnobstructed(
                        (foundEnt, targetXform),
                        (nearbyEnt, lightXform),
                        overloadAction.OverloadZapRadius,
                        CollisionGroup.MobMask))
                    continue;

                nearbyLights.Add(nearbyEnt);
            }

            if (nearbyLights.Count == 0)
                continue;

            // Find the closest light
            EntityUid? closestLight = null;
            var closestDistance = float.MaxValue;

            foreach (var light in nearbyLights)
            {
                var lightXform = Transform(light);
                if (!lightXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                    continue;

                if (!(distance < closestDistance))
                    continue;

                closestDistance = distance;
                closestLight = light;
            }

            if (closestLight is not { } closest)
                continue;

            EnsureComp<RevenantOverloadedLightsComponent>(closest).Target = foundEnt;
        }
    }

    private void OnMalfunctionAction(Entity<RevenantComponent> ent, ref RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RevenantActionComponent>(args.Action, out var revenantAction))
            return;

        if (!TryComp<MalfunctionActionComponent>(args.Action, out var malfunctionAction))
            return;

        if (!TryUseAbility(ent, revenantAction))
            return;

        args.Handled = true;

        foreach (var foundEnt in _lookup.GetEntitiesInRange(ent, malfunctionAction.MalfunctionRadius))
        {
            if (_whitelist.IsWhitelistFail(malfunctionAction.MalfunctionWhitelist, foundEnt) ||
                _whitelist.IsBlacklistPass(malfunctionAction.MalfunctionBlacklist, foundEnt))
                continue;

            _emag.DoEmagEffect(ent, foundEnt);
        }
    }
}
