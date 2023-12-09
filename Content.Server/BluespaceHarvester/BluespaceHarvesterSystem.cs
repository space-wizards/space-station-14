using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.Audio;
using Content.Shared.BluespaceHarvester;
using Content.Shared.Destructible;
using Content.Shared.Emag.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Audio.Systems;

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
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    // TODO: Move to component.
    private readonly List<BluespaceHarvesterTap> _taps = new()
    {
        new BluespaceHarvesterTap { Level = 0, Visual = BluespaceHarvesterVisuals.Tap0 },
        new BluespaceHarvesterTap { Level = 1, Visual = BluespaceHarvesterVisuals.Tap1 },
        new BluespaceHarvesterTap { Level = 5, Visual = BluespaceHarvesterVisuals.Tap2 },
        new BluespaceHarvesterTap { Level = 10, Visual = BluespaceHarvesterVisuals.Tap3 },
        new BluespaceHarvesterTap { Level = 15, Visual = BluespaceHarvesterVisuals.Tap4 },
        new BluespaceHarvesterTap { Level = 20, Visual = BluespaceHarvesterVisuals.Tap5 },
    };

    // Now we are updating all harvesters at the same time,
    // perhaps we should give each one an individual timer?
    private const float UpdateTime = 1.0f;
    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceHarvesterComponent, BluespaceHarvesterTargetLevelMessage>(OnTargetLevel);
        SubscribeLocalEvent<BluespaceHarvesterComponent, BluespaceHarvesterBuyMessage>(OnBuy);
        SubscribeLocalEvent<BluespaceHarvesterComponent, DestructionEventArgs>(OnDestruction);
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
            var ent = (uid, harvester);

            // We start only after manual switching on.
            if (harvester is { Reseted: false, CurrentLevel: 0 })
                harvester.Reseted = true;

            // The HV wires cannot transmit a lot of electricity so quickly,
            // which is why it will not start.
            // So this is simply using the amount of free electricity in the network.
            var supplier = GetPowerSupplier(ent);
            if (supplier < GetUsagePower(harvester.CurrentLevel) && harvester.CurrentLevel != 0)
            {
                // If there is insufficient production,
                // it will reset itself (turn off) and you will need to start it again,
                // this will not allow you to set it to maximum and enjoy life.
                Reset(ent);
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

            var generation = GetPointGeneration(ent);
            harvester.Points += generation;

            // They can be used for a table of records or to show off in front of friends;
            // initially in Space Station 13 this was necessary to complete the goal.
            harvester.TotalPoints += generation;

            // The generation of danger points can be negative, so there is this limitation here.
            harvester.Danger += GetDangerPointGeneration(ent);
            if (harvester.Danger < 0)
                harvester.Danger = 0;

            // If the danger points exceeded the DangerLimit and we were lucky enough to create a portal, then they will be created.
            if (harvester.Danger > harvester.DangerLimit && _random.NextFloat(0.0f, 1.0f) <= GetRiftChance(ent))
            {
                SpawnRifts((uid, harvester));
            }

            if (TryComp<AmbientSoundComponent>(uid, out var ambient))
                _ambientSound.SetAmbience(uid, harvester is { Reseted: true, CurrentLevel: > 0 }, ambient); // Bzhzh, bzhzh

            UpdateAppearance(ent);
            UpdateUI(ent);
        }
    }

    private void OnDestruction(Entity<BluespaceHarvesterComponent> ent, ref DestructionEventArgs args)
    {
        // This will not get rid of all the danger points by destroying the harvester, although it can still be disassembled.
        SpawnRifts(ent);
    }

    private void OnTargetLevel(Entity<BluespaceHarvesterComponent> ent, ref BluespaceHarvesterTargetLevelMessage args)
    {
        var user = args.Session.AttachedEntity;
        if (!Exists(user))
            return;

        // If we switch off, we don't need to be switched on.
        if (!ent.Comp.Reseted)
            return;

        ent.Comp.TargetLevel = args.TargetLevel;
        UpdateUI(ent);

        if (args.TargetLevel < ent.Comp.StableLevel)
        {
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value):player} set the level less than stable in {ToPrettyString(ent)} to {args.TargetLevel}");
            return;
        }

        if (args.TargetLevel == ent.Comp.StableLevel)
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} set the level equal to the stable in {ToPrettyString(ent)} to {args.TargetLevel}");
            return;
        }

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(user.Value):player} set the level MORE stable in {ToPrettyString(ent)} to {args.TargetLevel}");
    }

    private void OnBuy(Entity<BluespaceHarvesterComponent> ent, ref BluespaceHarvesterBuyMessage args)
    {
        var user = args.Session.AttachedEntity;
        if (!Exists(user))
            return;

        if (!ent.Comp.Reseted)
            return;

        if (!TryGetCategory(ent, args.Category, out var info))
            return;

        var category = (BluespaceHarvesterCategoryInfo) info;

        if (ent.Comp.Points < category.Cost)
            return;

        ent.Comp.Points -= category.Cost; // Damn capitalism.
        SpawnLoot(ent, category.PrototypeId);

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} buys in a {ToPrettyString(ent)}, {Enum.GetName(typeof(BluespaceHarvesterCategory), category.Type)} category");
    }

    private void UpdateAppearance(Entity<BluespaceHarvesterComponent> ent)
    {
        var level = ent.Comp.CurrentLevel;
        BluespaceHarvesterTap? max = null;

        foreach (var tap in _taps)
        {
            if (tap.Level > level)
                continue;

            if (max == null || tap.Level > max.Level)
                max = tap;
        }

        // We get the biggest Tap of all, and replace it with a harvester.
        if (max == null)
            return;

        _appearance.SetData(ent, BluespaceHarvesterVisualLayers.Base, (int)(Emagged(ent) ? ent.Comp.RedspaceTap : max.Visual));
        _appearance.SetData(ent, BluespaceHarvesterVisualLayers.Effects, level != 0);
    }

    private void UpdateUI(Entity<BluespaceHarvesterComponent> ent)
    {
        _ui.TrySetUiState(ent, BluespaceHarvesterUiKey.Key, new BluespaceHarvesterBoundUserInterfaceState(
            ent.Comp.TargetLevel,
            ent.Comp.CurrentLevel,
            ent.Comp.MaxLevel,
            GetUsagePower(ent.Comp.CurrentLevel),
            GetUsageNextPower(ent.Comp.CurrentLevel),
            GetPowerSupplier(ent),
            ent.Comp.Points,
            ent.Comp.TotalPoints,
            GetPointGeneration(ent),
            ent.Comp.Categories
        ));
    }

    private uint GetUsageNextPower(int level)
    {
        return GetUsagePower(level + 1);
    }

    private uint GetUsagePower(int level)
    {
        // TODO: Hopefully in the future you will need to put a mathematical formula or function here.
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
    private EntityUid? SpawnLoot(Entity<BluespaceHarvesterComponent> ent, string prototype)
    {
        var xform = Transform(ent);
        var coords = xform.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(ent.Comp.SpawnRadius));

        for (var i = 0; i < 20; i++)
        {
            var randVector = _random.NextVector2(ent.Comp.SpawnRadius);
            newCoords = coords.Offset(randVector);

            if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transform), LookupFlags.Static).Any())
                break;
        }

        _audio.PlayPvs(ent.Comp.SpawnSound, ent);
        Spawn(ent.Comp.SpawnEffect, newCoords);

        return Spawn(prototype, newCoords);
    }

    private int GetPointGeneration(Entity<BluespaceHarvesterComponent> ent)
    {
        return ent.Comp.CurrentLevel * 4 * (Emagged(ent) ? 2 : 1);
    }

    private int GetDangerPointGeneration(Entity<BluespaceHarvesterComponent> ent)
    {
        var stable = GetStableLevel(ent);

        if (ent.Comp.CurrentLevel == stable || ent.Comp.CurrentLevel == 0)
            return 0;

        if (ent.Comp.CurrentLevel < stable)
            return -1;

        return (ent.Comp.CurrentLevel - stable) * 4;
    }

    private float GetRiftChance(Entity<BluespaceHarvesterComponent> ent)
    {
        return Emagged(ent) ? ent.Comp.EmaggedRiftChance : ent.Comp.RiftChance;
    }

    private int GetStableLevel(Entity<BluespaceHarvesterComponent> ent)
    {
        return Emagged(ent) ? ent.Comp.EmaggedStableLevel : ent.Comp.StableLevel;
    }

    /// <summary>
    /// Receives information about all consumers and generators, subtracts and returns the amount of excess energy in the network.
    /// </summary>
    private float GetPowerSupplier(Entity<BluespaceHarvesterComponent> ent)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var nodeComp))
            return 0;

        if (!_nodeContainer.TryGetNode<Node>(nodeComp, "input", out var node))
            return 0;

        if (node.NodeGroup is not PowerNet netQ)
            return 0;

        var totalSources = 0.0f;
        foreach (var psc in netQ.Suppliers)
        {
            totalSources += psc.Enabled ? psc.MaxSupply : 0f;
        }

        foreach (var pcc in netQ.Dischargers)
        {
            if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            totalSources += batteryComp.NetworkBattery.CurrentSupply;
        }

        var totalConsumer = 0.0f;
        foreach (var pcc in netQ.Consumers)
        {
            totalConsumer += pcc.DrawRate;
        }

        foreach (var pcc in netQ.Chargers)
        {
            if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            totalConsumer += batteryComp.NetworkBattery.CurrentReceiving;
        }

        return totalSources - totalConsumer;
    }

    private bool TryGetCategory(Entity<BluespaceHarvesterComponent> ent, BluespaceHarvesterCategory target, [NotNullWhen(true)] out BluespaceHarvesterCategoryInfo? info)
    {
        info = null;
        foreach (var category in ent.Comp.Categories)
        {
            if (category.Type != target)
                continue;

            info = category;
            return true;
        }

        return false;
    }

    private void Reset(Entity<BluespaceHarvesterComponent> ent)
    {
        if (!ent.Comp.Reseted)
            return;

        ent.Comp.Danger += ent.Comp.DangerFromReset;
        ent.Comp.Reseted = false;
        ent.Comp.TargetLevel = 0;
    }

    private bool Emagged(EntityUid uid)
    {
        return HasComp<EmaggedComponent>(uid);
    }

    private void SpawnRifts(Entity<BluespaceHarvesterComponent> ent, int? danger = null)
    {
        var currentDanger = danger ?? ent.Comp.Danger;
        if (currentDanger == 0)
            return;

        var count = _random.Next(ent.Comp.RiftCount);
        for (var i = 0; i < count; i++)
        {
            // Haha loot!
            var entity = SpawnLoot(ent, ent.Comp.Rift);
            if (entity == null)
                continue;

            EnsureComp<BluespaceHarvesterRiftComponent>((EntityUid) entity).Danger = currentDanger / count;
        }

        // We gave all the danger to the rifts.
        ent.Comp.Danger = 0;
    }
}
