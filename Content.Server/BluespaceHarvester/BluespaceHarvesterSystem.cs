using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared.BluespaceHarvester;
using Content.Shared.Emag.Components;
using Microsoft.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.BluespaceHarvester;

public sealed class BluespaceHarvesterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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

        SubscribeLocalEvent<BluespaceHarvesterComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
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
            if (harvester.Enable)
            {
                if (harvester.CurrentLevel < harvester.TargetLevel)
                    harvester.CurrentLevel++;
            }

            if (harvester.CurrentLevel > harvester.TargetLevel)
                harvester.CurrentLevel--;

            consumer.DrawRate = GetUsagePower(harvester.CurrentLevel);

            var generation = GetPointGeneration(uid, harvester);
            harvester.Points += generation;
            harvester.TotalPoints += generation;
            harvester.DangerPoints += GetDangerPointGeneration(uid, harvester);

            if (harvester.DangerPoints < 0)
                harvester.DangerPoints = 0;

            UpdateAppearance(uid, harvester);
            UpdateUI(uid, harvester);
        }
    }

    private void OnPowerChanged(EntityUid uid, BluespaceHarvesterComponent component, PowerConsumerReceivedChanged args)
    {
        if (args.ReceivedPower < args.DrawRate)
        {
            // If there is insufficient production,
            // it will reset itself (turn off) and you will need to start it again,
            // this will not allow you to set it to maximum and enjoy life
            component.Enable = false;
            component.TargetLevel = 0;
        }

        UpdateAppearance(uid, component);
        UpdateUI(uid, component);
    }

    private void OnTargetLevel(EntityUid uid, BluespaceHarvesterComponent component, BluespaceHarvesterTargetLevelMessage args)
    {
        // If we switch off, we don't need to be switched on.
        if (!component.Enable && component.CurrentLevel != 0)
            return;

        component.TargetLevel = args.TargetLevel;
        component.Enable = true;
        UpdateUI(uid, component);
    }

    private void OnBuy(EntityUid uid, BluespaceHarvesterComponent component, BluespaceHarvesterBuyMessage args)
    {
        if (!component.Enable)
            return;

        var category = component.Categories.Find((e) => e.Type == args.Category);
        if (category.PrototypeId == null)
            return;

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
        var visuals = Taps.FindAll((tap) => tap.Level <= level);

        if (visuals.Count == 0)
            return;

        var max = visuals.MaxBy((tap) => tap.Level);
        if (max == null)
            return;

        if (HasComp<EmaggedComponent>(uid))
        {
            _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Base, (int) harvester.RedspaceTap);
        }
        else
        {
            _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Base, (int) max.Visual);
        }

        _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Effects, level != 0);
    }

    private void UpdateUI(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        var powerUsage = GetUsagePower(harvester.CurrentLevel);
        var powerUsageNext = GetUsagePower(harvester.CurrentLevel + 1);

        _ui.TrySetUiState(uid, BluespaceHarvesterUiKey.Key, new BluespaceHarvesterBoundUserInterfaceState(
            harvester.TargetLevel,
            harvester.CurrentLevel,
            harvester.MaxLevel,
            powerUsage,
            powerUsageNext,
            GetPowerSupplier(uid, harvester),
            harvester.Points,
            harvester.TotalPoints,
            GetPointGeneration(uid, harvester),
            harvester.Categories
        ));
    }

    public uint GetUsageNextPower(int level)
    {
        return GetUsagePower(level + 1);
    }

    public uint GetUsagePower(int level)
    {
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
            8 => 5_000_000,
            9 => 10_000_000,
            10 => 20_000_000,
            11 => 50_000_000,
            12 => 100_000_000,
            13 => 200_000_000,
            14 => 400_000_000,
            15 => 800_000_000,
            16 => 1_000_000_000,
            17 => 2_000_000_000,
            //18 => 5_000_000_000,
            //19 => 10_000_000_000,
            //20 => 20_000_000_000,
            _ => 0,
        };
    }

    private void SpawnLoot(EntityUid uid, string prototype, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        var xform = Transform(uid);
        var coords = xform.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(harvester.SpawnRadius));

        for (var i = 0; i < 20; i++)
        {
            var randVector = _random.NextVector2(harvester.SpawnRadius);
            newCoords = coords.Offset(randVector);
            if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transform), LookupFlags.Static).Any())
            {
                break;
            }
        }

        Spawn(prototype, newCoords);
    }

    public int GetPointGeneration(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return harvester.CurrentLevel * 4 * (HasComp<EmagComponent>(uid) ? 2 : 1);
    }

    public int GetDangerPointGeneration(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        var stable = GetStableLevel(uid, harvester);
        if (harvester.CurrentLevel < stable)
            return -4;

        return  Math.Abs(harvester.CurrentLevel - harvester.MaxLevel) * 2;
    }

    public int GetStableLevel(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return HasComp<EmaggedComponent>(uid) ? harvester.EmaggedStableLevel : harvester.StableLevel;
    }

    public float GetPowerSupplier(EntityUid uid, BluespaceHarvesterComponent? harvester = null, NodeContainerComponent? nodeComp = null)
    {
        if (!Resolve(uid, ref harvester, ref nodeComp))
            return 0;

        if (!_nodeContainer.TryGetNode<Node>(nodeComp, "input", out var node))
            return 0;

        if (node.NodeGroup is not PowerNet netQ)
            return 0;

        var totalSources = 0.0f;
        foreach (PowerSupplierComponent pcc in netQ.Suppliers)
        {
            var supply = pcc.Enabled
                ? pcc.MaxSupply
                : 0f;

            totalSources += supply;
        }

        return totalSources;
    }
}
