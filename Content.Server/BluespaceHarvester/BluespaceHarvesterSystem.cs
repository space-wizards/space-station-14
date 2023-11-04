using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.Audio;
using Content.Shared.BluespaceHarvester;
using Content.Shared.Emag.Components;
using Microsoft.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.BluespaceHarvester;

public sealed class BluespaceHarvesterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;

    public List<BluespaceHarvesterTap> Taps = new List<BluespaceHarvesterTap>()
    {
        new BluespaceHarvesterTap() { Level = 0, Visual = BluespaceHarvesterVisuals.Tap0 },
        new BluespaceHarvesterTap() { Level = 1, Visual = BluespaceHarvesterVisuals.Tap1 },
        new BluespaceHarvesterTap() { Level = 5, Visual = BluespaceHarvesterVisuals.Tap2 },
        new BluespaceHarvesterTap() { Level = 10, Visual = BluespaceHarvesterVisuals.Tap3 },
        new BluespaceHarvesterTap() { Level = 15, Visual = BluespaceHarvesterVisuals.Tap4 },
        new BluespaceHarvesterTap() { Level = 20, Visual = BluespaceHarvesterVisuals.Tap5 },
    };

    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceHarvesterComponent, BluespaceHarvesterTargetLevelMessage>(OnTargetLevel);
        SubscribeLocalEvent<BluespaceHarvesterComponent, BluespaceHarvesterBuyMessage>(OnBuy);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateTime)
            return;
        _updateTimer -= UpdateTime;

        var query = EntityQueryEnumerator<BluespaceHarvesterComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var harvester, out var consumer))
        {
            // The HV wires cannot transmit a lot of electricity so quickly,
            // which is why it will not start.
            // So this is simply using the amount of free electricity in the network.
            var supplier = GetPowerSupplier(uid, harvester);

            if (supplier < GetUsagePower(harvester.CurrentLevel))
            {
                // If there is insufficient production,
                // it will reset itself (turn off) and you will need to start it again,
                // this will not allow you to set it to maximum and enjoy life
                Reset(uid, harvester);
            }

            if (harvester.Reseted)
            {
                if (harvester.CurrentLevel < harvester.TargetLevel)
                    harvester.CurrentLevel++;
            }

            if (harvester.CurrentLevel > harvester.TargetLevel)
                harvester.CurrentLevel--;

            // Increasing the amount of energy regardless of its ability to generate it
            // will make it impossible to set the desired value and go to rest.
            consumer.DrawRate = GetUsagePower(harvester.CurrentLevel);

            var generation = GetPointGeneration(uid, harvester);
            harvester.Points += generation;
            harvester.TotalPoints += generation;

            // the generation of danger points can be negative, so there is this limitation here.
            harvester.Danger += GetDangerPointGeneration(uid, harvester);
            if (harvester.Danger < 0)
                harvester.Danger = 0;

            // If the danger points exceeded the DangerLimit and we were lucky enough to create a portal, then they will be created.
            if (harvester.Danger > harvester.DangerLimit && _random.NextFloat(0.0f, 1.0f) <= GetRiftChance(uid, harvester))
            {
                var count = _random.Next(3);
                for (var i = 0; i < count; i++)
                {
                    // Haha loot!
                    var entity = SpawnLoot(uid, harvester.Rift, harvester);
                    if (entity == null)
                        continue;

                    EnsureComp<BluespaceHarvesterRiftComponent>((EntityUid) entity).Danger = harvester.Danger / 3;
                }

                // We gave all the danger to the rifts.
                harvester.Danger = 0;
            }

            if (TryComp<AmbientSoundComponent>(uid, out var ambient))
                _ambientSound.SetAmbience(uid, harvester.Reseted, ambient); // Bzhzh, bzhzh

            UpdateAppearance(uid, harvester);
            UpdateUI(uid, harvester);
        }
    }

    private void OnTargetLevel(EntityUid uid, BluespaceHarvesterComponent component, BluespaceHarvesterTargetLevelMessage args)
    {
        // If we switch off, we don't need to be switched on.
        if (!component.Reseted && component.CurrentLevel != 0)
            return;

        component.TargetLevel = args.TargetLevel;
        component.Reseted = true;
        UpdateUI(uid, component);
    }

    private void OnBuy(EntityUid uid, BluespaceHarvesterComponent component, BluespaceHarvesterBuyMessage args)
    {
        if (!component.Reseted)
            return;

        if (!TryGetCategory(uid, args.Category, out var info, component))
            return;

        var category = (BluespaceHarvesterCategoryInfo) info;

        if (component.Points < category.Cost)
            return;

        component.Points -= category.Cost;
        SpawnLoot(uid, category.PrototypeId, component);
    }

    private void UpdateAppearance(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        var level = harvester.CurrentLevel;
        BluespaceHarvesterTap? max = null;

        foreach (var tap in Taps)
        {
            if (tap.Level > level)
                continue;

            if (max == null || tap.Level > max.Level)
                max = tap;
        }

        // We get the biggest Tap of all, and replace it with a harvester.
        if (max == null)
            return;

        if (Emagged(uid))
            _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Base, (int) harvester.RedspaceTap);
        else
            _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Base, (int) max.Visual);

        _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Effects, level != 0);
    }

    private void UpdateUI(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        _ui.TrySetUiState(uid, BluespaceHarvesterUiKey.Key, new BluespaceHarvesterBoundUserInterfaceState(
            harvester.TargetLevel,
            harvester.CurrentLevel,
            harvester.MaxLevel,
            GetUsagePower(harvester.CurrentLevel),
            GetUsageNextPower(harvester.CurrentLevel),
            GetPowerSupplier(uid, harvester),
            harvester.Points,
            harvester.TotalPoints,
            GetPointGeneration(uid, harvester),
            harvester.Categories
        ));
    }

    private uint GetUsageNextPower(int level)
    {
        return GetUsagePower(level + 1);
    }

    private uint GetUsagePower(int level)
    {
        // Hopefully in the future you will need to put a mathematical formula or function here.
        return level switch
        {
            0 => 500,
            1 => 1_000,
            2 => 5_000,
            3 => 50_000,
            4 => 100_000,
            5 => 500_000,
            6 => 1_000_000,
            7 => 2_000_000,
            8 => 3_000_000,
            9 => 5_000_000,
            10 => 7_000_000,
            11 => 9_000_000,
            12 => 10_000_000,
            13 => 12_000_000,
            14 => 14_000_000,
            15 => 16_000_000,
            16 => 20_000_000,
            17 => 40_000_000,
            18 => 80_000_000,
            19 => 100_000_000,
            20 => 200_000_000,
            _ => 1_000_000_000,
        };
    }

    /// <summary>
    /// Finds a free point in space and creates a prototype there, similar to a bluespace anomaly.
    /// </summary>
    private EntityUid? SpawnLoot(EntityUid uid, string prototype, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return null;

        var xform = Transform(uid);
        var coords = xform.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(harvester.SpawnRadius));

        for (var i = 0; i < 20; i++)
        {
            var randVector = _random.NextVector2(harvester.SpawnRadius);
            newCoords = coords.Offset(randVector);

            if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transform), LookupFlags.Static).Any())
                break;
        }

        _audio.PlayPvs(harvester.SpawnSound, uid);
        Spawn(harvester.SpawnEffect, newCoords);

        return Spawn(prototype, newCoords);
    }

    private int GetPointGeneration(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return harvester.CurrentLevel * 4 * (Emagged(uid) ? 2 : 1);
    }

    private int GetDangerPointGeneration(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        var stable = GetStableLevel(uid, harvester);

        if (harvester.CurrentLevel < stable)
            return -4;

        if (harvester.CurrentLevel == stable)
            return 0;

        return (harvester.CurrentLevel - stable) * 4;
    }

    private float GetRiftChance(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return Emagged(uid) ? harvester.EmaggedRiftChance : harvester.RiftChance;
    }

    private int GetStableLevel(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return Emagged(uid) ? harvester.EmaggedStableLevel : harvester.StableLevel;
    }

    /// <summary>
    /// Receives information about all consumers and generators, subtracts and returns the amount of excess energy in the network.
    /// </summary>
    private float GetPowerSupplier(EntityUid uid, BluespaceHarvesterComponent? harvester = null, NodeContainerComponent? nodeComp = null)
    {
        if (!Resolve(uid, ref harvester, ref nodeComp))
            return 0;

        if (!_nodeContainer.TryGetNode<Node>(nodeComp, "input", out var node))
            return 0;

        if (node.NodeGroup is not PowerNet netQ)
            return 0;

        var totalSources = 0.0f;
        foreach (PowerSupplierComponent psc in netQ.Suppliers)
        {
            var supply = psc.Enabled
                ? psc.MaxSupply
                : 0f;

            totalSources += supply;
        }

        foreach (BatteryDischargerComponent pcc in netQ.Dischargers)
        {
            if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentSupply;
            totalSources += rate;
        }

        var totalConsumer = 0.0f;
        foreach (PowerConsumerComponent pcc in netQ.Consumers)
        {
            var consume = pcc.DrawRate;
            totalConsumer += consume;
        }

        foreach (BatteryChargerComponent pcc in netQ.Chargers)
        {
            if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentReceiving;
            totalConsumer += rate;
        }

        return totalSources - totalConsumer;
    }

    private bool TryGetCategory(EntityUid uid, BluespaceHarvesterCategory target, [NotNullWhen(true)] out BluespaceHarvesterCategoryInfo? info, BluespaceHarvesterComponent? harvester = null)
    {
        info = null;
        if (!Resolve(uid, ref harvester))
            return false;

        foreach (var category in harvester.Categories)
        {
            if (category.Type != target)
                continue;

            info = category;
            return true;
        }

        return false;
    }

    private void Reset(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        harvester.Reseted = true;
        harvester.TargetLevel = 0;
    }

    private bool Emagged(EntityUid uid)
    {
        return HasComp<EmagComponent>(uid);
    }
}
