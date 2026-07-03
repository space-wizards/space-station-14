using System.Linq;
using Content.Shared.Ame.Components;
using Content.Shared.Chat;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Components;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Shared.Ame.Systems;

public sealed partial class AmeNodeGroupHandler : SingleNodeGroupHandler<AmeNodeGroupComponent>
{
    [Dependency] private ISharedChatManager _chat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAmeControllerSystem _ameControllerSystem = default!;
    [Dependency] private SharedExplosionSystem _explosionSystem = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private AmeShieldingSystem _ameShieldingSystem = default!;
    [Dependency] private EntityQuery<AmeShieldComponent> _shieldQuery = default!;
    [Dependency] private EntityQuery<AmeControllerComponent> _controllerQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;

    protected override NodeGroupID NodeGroupID => NodeGroupID.AMEngine;

    protected override void LoadNodes(Entity<NodeGroupComponent, AmeNodeGroupComponent> group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);
        var groupComp = group.Comp2;

        EntityUid? gridEnt = null;
        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!_shieldQuery.TryComp(nodeOwner, out var shield))
                continue;

            var xform = Transform(nodeOwner);
            if (!_mapGridQuery.TryComp(xform.GridUid, out var grid))
                continue;

            if (gridEnt == null)
                gridEnt = xform.GridUid;
            else if (gridEnt != xform.GridUid)
                continue;

            var nodeNeighbors = _mapSystem.GetCellsInSquareArea(xform.GridUid.Value, grid, xform.Coordinates, 1)
                .Where(entity => entity != nodeOwner && _shieldQuery.HasComp(entity));

            if (nodeNeighbors.Count() >= 8)
            {
                groupComp.Cores.Add(nodeOwner);
                _ameShieldingSystem.SetCore((nodeOwner, shield), true);
                // Core visuals will be updated later.
            }
            else
            {
                _ameShieldingSystem.SetCore((nodeOwner, shield), false);
            }
        }

        // Separate to ensure core count is correctly updated.
        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!_controllerQuery.TryComp(nodeOwner, out var controller))
                continue;

            groupComp.MasterController ??= nodeOwner;
            _ameControllerSystem.UpdateUi((nodeOwner, controller));
        }

        UpdateCoreVisuals((group.Owner, group.Comp2));
    }

    public void UpdateCoreVisuals(Entity<AmeNodeGroupComponent> group)
    {
        var groupComp = group.Comp;
        var injectionAmount = 0;
        var injecting = false;

        if (_controllerQuery.TryComp(groupComp.MasterController, out var controller))
        {
            injectionAmount = controller.InjectionAmount;
            injecting = controller.Injecting;
        }

        var injectionStrength = groupComp.Cores.Count > 0 ? injectionAmount / groupComp.Cores.Count : 0;

        foreach (var coreUid in groupComp.Cores)
        {
            _ameShieldingSystem.UpdateCoreVisuals(coreUid, injectionStrength, injecting);
        }
    }

    public float InjectFuel(Entity<AmeNodeGroupComponent> group, int fuel, out bool overloading)
    {
        overloading = false;

        var groupComp = group.Comp;
        if (fuel <= 0 || groupComp.Cores.Count <= 0)
            return 0;

        var safeFuelLimit = groupComp.Cores.Count * 2;

        var powerOutput = CalculatePower(fuel, groupComp.Cores.Count);
        if (fuel <= safeFuelLimit)
            return powerOutput;

        // The AME is being overloaded.
        // Note about these maths: I would assume the general idea here is to make larger engines less safe to overload.
        // In other words, yes, those are supposed to be group.Cores.Count, not safeFuelLimit.
        var overloadVsSizeResult = fuel - groupComp.Cores.Count;

        var instability = overloadVsSizeResult / groupComp.Cores.Count;
        var fuzz = _random.Next(-1, 2); // -1 to 1
        instability += fuzz; // fuzz the values a tiny bit.

        overloading = true;
        var integrityCheck = 100;
        foreach (var coreUid in groupComp.Cores)
        {
            if (!_shieldQuery.TryComp(coreUid, out var core))
                continue;

            var oldIntegrity = core.CoreIntegrity;
            core.CoreIntegrity -= instability;

            if (oldIntegrity > 95
                && core.CoreIntegrity <= 95
                && core.CoreIntegrity < integrityCheck)
                integrityCheck = core.CoreIntegrity;
        }

        // Admin alert
        if (integrityCheck != 100 && groupComp.MasterController.HasValue)
            _chat.SendAdminAlert($"AME overloading: {ToPrettyString(groupComp.MasterController.Value)}");

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

    public int GetTotalStability(Entity<AmeNodeGroupComponent> group)
    {
        var groupComp = group.Comp;
        if (groupComp.Cores.Count < 1)
            return 100;

        var stability = 0;
        foreach (var coreUid in groupComp.Cores)
        {
            if (_shieldQuery.TryComp(coreUid, out var core))
                stability += core.CoreIntegrity;
        }

        stability /= groupComp.Cores.Count;

        return stability;
    }

    public void ExplodeCores(Entity<AmeNodeGroupComponent> group)
    {
        var groupComp = group.Comp;
        if (groupComp.Cores.Count < 1
            || !_controllerQuery.TryComp(groupComp.MasterController, out var controller))
            return;

        /*
            * todo: add an exact to the shielding and make this find the core closest to the controller
            * so they chain explode, after helpers have been added to make it not cancer
        */
        var radius = Math.Min(2 * groupComp.Cores.Count * controller.InjectionAmount, 8f);
        _explosionSystem.TriggerExplosive(groupComp.MasterController.Value, radius: radius, delete: false);
    }
}
