using System.Linq;
using Content.Server.Ame.Components;
using Content.Server.Chat.Managers;
using Content.Shared.Ame;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Ame.EntitySystems;

public sealed partial class AmeNodeGroupHandler : SingleNodeGroupHandler<AmeNodeGroup>
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AmeControllerSystem _ameControllerSystem = default!;
    [Dependency] private AmeShieldingSystem _ameShieldingSystem = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private EntityQuery<AmeShieldComponent> _shieldQuery = default!;
    [Dependency] private EntityQuery<AmeControllerComponent> _controllerQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;

    protected override NodeGroupID NodeGroupID => NodeGroupID.AMEngine;

    protected override void LoadNodes(AmeNodeGroup group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);
        EntityUid? gridEnt = null;

        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!_shieldQuery.TryComp(nodeOwner, out var shield))
                continue;
            if (!_xformQuery.TryComp(nodeOwner, out var xform))
                continue;
            if (!_mapGridQuery.TryComp(xform.GridUid, out var grid))
                continue;

            if (gridEnt == null)
                gridEnt = xform.GridUid;
            else if (gridEnt != xform.GridUid)
                continue;

            var nodeNeighbors = _mapSystem.GetCellsInSquareArea(xform.GridUid.Value, grid, xform.Coordinates, 1)
                .Where(entity => entity != nodeOwner && _shieldQuery.HasComponent(entity));

            if (nodeNeighbors.Count() >= 8)
            {
                group.Cores.Add(nodeOwner);
                _ameShieldingSystem.SetCore(nodeOwner, true, shield);
                // Core visuals will be updated later.
            }
            else
            {
                _ameShieldingSystem.SetCore(nodeOwner, false, shield);
            }
        }

        // Separate to ensure core count is correctly updated.
        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!_controllerQuery.TryGetComponent(nodeOwner, out var controller))
                continue;

            group.MasterController ??= nodeOwner;
            _ameControllerSystem.UpdateUi(nodeOwner, controller);
        }

        UpdateCoreVisuals(group);
    }

    public void UpdateCoreVisuals(AmeNodeGroup group)
    {
        var injectionAmount = 0;
        var injecting = false;

        if (_entMan.TryGetComponent<AmeControllerComponent>(group.MasterController, out var controller))
        {
            injectionAmount = controller.InjectionAmount;
            injecting = controller.Injecting;
        }

        var injectionStrength = group.Cores.Count > 0 ? injectionAmount / group.Cores.Count : 0;

        var coreSystem = _entMan.System<AmeShieldingSystem>();
        foreach (var coreUid in group.Cores)
        {
            coreSystem.UpdateCoreVisuals(coreUid, injectionStrength, injecting);
        }
    }

    public float InjectFuel(AmeNodeGroup group, int fuel, out bool overloading)
    {
        overloading = false;

        var shieldQuery = _entMan.GetEntityQuery<AmeShieldComponent>();
        if (fuel <= 0 || group.Cores.Count <= 0)
            return 0;

        var safeFuelLimit = group.Cores.Count * 2;

        var powerOutput = CalculatePower(fuel, group.Cores.Count);
        if (fuel <= safeFuelLimit)
            return powerOutput;

        // The AME is being overloaded.
        // Note about these maths: I would assume the general idea here is to make larger engines less safe to overload.
        // In other words, yes, those are supposed to be group.Cores.Count, not safeFuelLimit.
        var overloadVsSizeResult = fuel - group.Cores.Count;

        var instability = overloadVsSizeResult / group.Cores.Count;
        var fuzz = _random.Next(-1, 2); // -1 to 1
        instability += fuzz; // fuzz the values a tiny bit.

        overloading = true;
        var integrityCheck = 100;
        foreach (var coreUid in group.Cores)
        {
            if (!shieldQuery.TryGetComponent(coreUid, out var core))
                continue;

            var oldIntegrity = core.CoreIntegrity;
            core.CoreIntegrity -= instability;

            if (oldIntegrity > 95
                && core.CoreIntegrity <= 95
                && core.CoreIntegrity < integrityCheck)
                integrityCheck = core.CoreIntegrity;
        }

        // Admin alert
        if (integrityCheck != 100 && group.MasterController.HasValue)
            _chat.SendAdminAlert($"AME overloading: {_entMan.ToPrettyString(group.MasterController.Value)}");

        return powerOutput;
    }

    /// <summary>
    /// Calculates the amount of power the AME can produce with the given settings
    /// </summary>
    public float CalculatePower(int fuel, int cores)
    {
        // Balanced around a single core AME with injection level 2 producing 120KW.
        // Two core with four injection is 150kW. Two core with two injection is 90kW.

        // Increasing core count creates diminishing returns, increasing injection amount increases
        // Unlike the previous solution, increasing fuel and cores always leads to an increase in power, even if by very small amounts.
        // Increasing core count without increasing fuel always leads to reduced power as well.
        // At 18+ cores and 2 inject, the power produced is less than 0, the Max ensures the AME can never produce "negative" power.
        return MathF.Max(200000f * MathF.Log10(2 * fuel * MathF.Pow(cores, (float)-0.5)), 0);
    }

    public int GetTotalStability(AmeNodeGroup group)
    {
        if (group.Cores.Count < 1)
            return 100;

        var stability = 0;
        var coreQuery = _entMan.GetEntityQuery<AmeShieldComponent>();
        foreach (var coreUid in group.Cores)
        {
            if (coreQuery.TryGetComponent(coreUid, out var core))
                stability += core.CoreIntegrity;
        }

        stability /= group.Cores.Count;

        return stability;
    }

    public void ExplodeCores(AmeNodeGroup group)
    {
        if (group.Cores.Count < 1
        || !_entMan.TryGetComponent<AmeControllerComponent>(group.MasterController, out var controller))
            return;

        /*
            * todo: add an exact to the shielding and make this find the core closest to the controller
            * so they chain explode, after helpers have been added to make it not cancer
        */
        var radius = Math.Min(2 * group.Cores.Count * controller.InjectionAmount, 8f);
        _entMan.System<SharedExplosionSystem>().TriggerExplosive(group.MasterController.Value, radius: radius, delete: false);
    }
}
