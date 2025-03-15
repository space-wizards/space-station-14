using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Revenant.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.Systems;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Abilities;

/// <summary>
///     This handles general abilities.
/// </summary>
/// <remarks>Mostly used for the revenant abilities for now, so that we can reuse them in other code.</remarks>
public sealed class AbilitySystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedRevenantOverloadedLightsSystem _overloadedLights = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    public override void Initialize()
    {
        // TODO: Ideally these would be by-ref when action events allow for it
        SubscribeLocalEvent<RevenantDefileActionEvent>(OnDefileAction);
        SubscribeLocalEvent<RevenantOverloadLightsActionEvent>(OnOverloadLightsAction);
        SubscribeLocalEvent<RevenantMalfunctionActionEvent>(OnMalfunctionAction);
        SubscribeLocalEvent<RevenantColdSnapActionEvent>(OnColdSnapAction);
        SubscribeLocalEvent<RevenantEnergyDrainActionEvent>(OnEnergyDrainAction);
    }

    private void OnDefileAction(RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DefileActionComponent>(args.Action, out var defileAction))
            return;

        args.Handled = true;

        Defile(args.Performer, (args.Action, defileAction));
    }

    private void OnOverloadLightsAction(RevenantOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<OverloadLightsActionComponent>(args.Action, out var overloadAction))
            return;

        args.Handled = true;

        OverloadLights(args.Performer, (args.Action, overloadAction));
    }

    private void OnMalfunctionAction(RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MalfunctionActionComponent>(args.Action, out var malfunctionAction))
            return;

        args.Handled = true;

        Malfunction(args.Performer, (args.Action, malfunctionAction));
    }

    private void OnColdSnapAction(RevenantColdSnapActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ColdSnapActionComponent>(args.Action, out var coldSnapAction))
            return;

        args.Handled = true;

        ColdSnap(args.Performer, (args.Action, coldSnapAction));
    }

    private void OnEnergyDrainAction(RevenantEnergyDrainActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<EnergyDrainActionComponent>(args.Action, out var energyDrainAction))
            return;

        args.Handled = true;

        EnergyDrain(args.Performer, (args.Action, energyDrainAction));
    }

    /// <summary>
    ///     Defiles an area around the user, breaking tiles and causing chaos.
    /// </summary>
    public void Defile(EntityUid user, Entity<DefileActionComponent> ability)
    {
        var xform = Transform(user);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;

        var tiles = _map.GetTilesIntersecting(
                xform.GridUid.Value,
                map,
                Box2.CenteredAround(_xform.GetWorldPosition(xform),
                    new Vector2(ability.Comp.DefileRadius * 2, ability.Comp.DefileRadius)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < ability.Comp.DefileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(user,
            ability.Comp.DefileRadius,
            LookupFlags.Approximate | LookupFlags.Static);
        var tags = GetEntityQuery<TagComponent>();
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var foundEnt in lookup)
        {
            if (tags.HasComponent(foundEnt) && _tag.HasTag(foundEnt, "Window"))
            {
                var dspec = new DamageSpecifier();
                dspec.DamageDict.Add("Structural", 60);
                _damage.TryChangeDamage(foundEnt, dspec, origin: user);
            }

            if (!_random.Prob(ability.Comp.DefileEffectChance))
                continue;

            if (entityStorage.HasComponent(foundEnt))
                _entityStorage.OpenStorage(foundEnt);

            if (items.HasComponent(foundEnt) &&
                TryComp<PhysicsComponent>(foundEnt, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(foundEnt, _random.NextAngle().ToWorldVec());

            if (lights.HasComponent(foundEnt))
                _ghost.DoGhostBooEvent(foundEnt);
        }
    }

    /// <summary>
    ///     Drains energy from all batteries in range.
    /// </summary>
    public void EnergyDrain(EntityUid user, Entity<EnergyDrainActionComponent> ability)
    {
        var lookup = _lookup.GetEntitiesInRange(user, ability.Comp.DrainRadius);
        var batteryQuery = GetEntityQuery<BatteryComponent>();

        foreach (var ent in lookup)
        {
            if (!batteryQuery.TryGetComponent(ent, out var battery))
                continue;

            var drainAmount = battery.CurrentCharge * ability.Comp.DrainFraction;
            _battery.SetCharge(ent, battery.CurrentCharge - drainAmount, battery);
        }
    }

    /// <summary>
    ///     Overloads lights near targets in range, causing them to zap nearby entities.
    /// </summary>
    public void OverloadLights(EntityUid user, Entity<OverloadLightsActionComponent> ability)
    {
        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var mobState = GetEntityQuery<MobStateComponent>();
        var lookup = _lookup.GetEntitiesInRange(user, ability.Comp.OverloadRadius);

        foreach (var foundEnt in lookup)
        {
            if (!mobState.HasComponent(foundEnt) || !_mobState.IsAlive(foundEnt))
                continue;

            var targetXform = Transform(foundEnt);
            var nearbyLights = new List<EntityUid>();
            var nearbyEntities = _lookup.GetEntitiesInRange(foundEnt, ability.Comp.OverloadZapRadius);

            foreach (var nearbyEnt in nearbyEntities)
            {
                if (!poweredLights.HasComponent(nearbyEnt) ||
                    HasComp<RevenantOverloadedLightsComponent>(nearbyEnt))
                    continue;

                var lightXform = Transform(nearbyEnt);
                if (!_interact.InRangeUnobstructed(
                        (foundEnt, targetXform),
                        (nearbyEnt, lightXform),
                        ability.Comp.OverloadZapRadius,
                        CollisionGroup.MobMask))
                    continue;

                nearbyLights.Add(nearbyEnt);
            }

            if (nearbyLights.Count == 0)
                continue;

            EntityUid? closestLight = null;
            var closestDistance = float.MaxValue;
            var userXform = Transform(user);

            foreach (var light in nearbyLights)
            {
                var lightXform = Transform(light);
                if (!lightXform.Coordinates.TryDistance(EntityManager, userXform.Coordinates, out var distance))
                    continue;

                if (!(distance < closestDistance))
                    continue;

                closestDistance = distance;
                closestLight = light;
            }

            if (closestLight is not { } closest)
                continue;

            var comp = EnsureComp<RevenantOverloadedLightsComponent>(closest);
            _overloadedLights.SetZapTarget((closest, comp), foundEnt);
        }
    }

    /// <summary>
    /// Creates a cold snap that reduces atmospheric temperature in the area.
    /// </summary>
    public void ColdSnap(EntityUid user, Entity<ColdSnapActionComponent> ability)
    {
        var xform = Transform(user);
        var mixtures = new List<Vector2i>();

        // Get all nearby tiles
        for (var x = -ability.Comp.ColdSnapRadius; x <= ability.Comp.ColdSnapRadius; x++)
        {
            for (var y = -ability.Comp.ColdSnapRadius; y <= ability.Comp.ColdSnapRadius; y++)
            {
                if (x * x + y * y > ability.Comp.ColdSnapRadius * ability.Comp.ColdSnapRadius)
                    continue;

                var tile = _xform.GetGridTilePositionOrDefault((user, xform));
                tile.X += (int) x;
                tile.Y += (int) y;
                mixtures.Add(tile);
            }
        }

        // Get all gas mixtures at these tiles
        var gasMixtures = _atmos.GetTileMixtures(xform.GridUid, xform.MapUid, mixtures, true);
        if (gasMixtures == null)
            return;

        // Remove energy from each gas mixture
        foreach (var mixture in gasMixtures)
        {
            if (mixture == null)
                continue;

            _atmos.AddHeat(mixture, ability.Comp.EnergyChange);
        }
    }

    /// <summary>
    ///     Causes electrical malfunctions in devices within range.
    /// </summary>
    public void Malfunction(EntityUid user, Entity<MalfunctionActionComponent> ability)
    {
        foreach (var foundEnt in _lookup.GetEntitiesInRange(user, ability.Comp.MalfunctionRadius))
        {
            if (_whitelist.IsWhitelistFail(ability.Comp.MalfunctionWhitelist, foundEnt) ||
                _whitelist.IsBlacklistPass(ability.Comp.MalfunctionBlacklist, foundEnt))
                continue;

            var ev = new GotEmaggedEvent(user, EmagType.Interaction | EmagType.Access);
            RaiseLocalEvent(foundEnt, ref ev);
        }
    }
}
