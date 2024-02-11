using System.Linq;
using Content.Server.Ame.Components;
using Content.Server.Ame.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Ame;

/// <summary>
/// Node group class for handling the Antimatter Engine's console and parts.
/// </summary>
[NodeGroup(NodeGroupID.AMEngine)]
public sealed class AmeNodeGroup : BaseNodeGroup
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// The AME controller which is currently in control of this node group.
    /// This could be tracked a few different ways, but this is most convenient,
    /// since any part connected to the node group can easily find the master.
    /// </summary>
    [ViewVariables]
    private EntityUid? _masterController;

    public EntityUid? MasterController => _masterController;

    /// <summary>
    /// The set of AME shielding units that currently count as cores for the AME.
    /// </summary>
    private readonly List<EntityUid> _cores = new();

    public int CoreCount => _cores.Count;

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        EntityUid? gridEnt = null;

        var ameControllerSystem = _entMan.System<AmeControllerSystem>();
        var ameShieldingSystem = _entMan.System<AmeShieldingSystem>();

        var shieldQuery = _entMan.GetEntityQuery<AmeShieldComponent>();
        var controllerQuery = _entMan.GetEntityQuery<AmeControllerComponent>();
        var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!shieldQuery.TryGetComponent(nodeOwner, out var shield))
                continue;
            if (!xformQuery.TryGetComponent(nodeOwner, out var xform))
                continue;
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                continue;

            if (gridEnt == null)
                gridEnt = xform.GridUid;
            else if (gridEnt != xform.GridUid)
                continue;

            var nodeNeighbors = grid.GetCellsInSquareArea(xform.Coordinates, 1)
                .Where(entity => entity != nodeOwner && shieldQuery.HasComponent(entity));

            if (nodeNeighbors.Count() >= 8)
            {
                _cores.Add(nodeOwner);
                ameShieldingSystem.SetCore(nodeOwner, true, shield);
                // Core visuals will be updated later.
            }
            else
            {
                ameShieldingSystem.SetCore(nodeOwner, false, shield);
            }
        }

        // Separate to ensure core count is correctly updated.
        foreach (var node in groupNodes)
        {
            var nodeOwner = node.Owner;
            if (!controllerQuery.TryGetComponent(nodeOwner, out var controller))
                continue;

            if (_masterController == null)
                _masterController = nodeOwner;

            ameControllerSystem.UpdateUi(nodeOwner, controller);
        }

        UpdateCoreVisuals();
    }

    public void UpdateCoreVisuals()
    {
        var injectionAmount = 0;
        var injecting = false;

        if (_entMan.TryGetComponent<AmeControllerComponent>(_masterController, out var controller))
        {
            injectionAmount = controller.InjectionAmount;
            injecting = controller.Injecting;
        }

        var injectionStrength = CoreCount > 0 ? injectionAmount / CoreCount : 0;

        var coreSystem = _entMan.System<AmeShieldingSystem>();
        foreach (var coreUid in _cores)
        {
            coreSystem.UpdateCoreVisuals(coreUid, injectionStrength, injecting);
        }
    }

    public float InjectFuel(int fuel, out bool overloading)
    {
        overloading = false;

        var shieldQuery = _entMan.GetEntityQuery<AmeShieldComponent>();
        if (fuel <= 0 || CoreCount <= 0)
            return 0;

        var safeFuelLimit = CoreCount * 2;

        var powerOutput = CalculatePower(fuel, CoreCount);
        if (fuel <= safeFuelLimit)
            return powerOutput;

        // The AME is being overloaded.
        // Note about these maths: I would assume the general idea here is to make larger engines less safe to overload.
        // In other words, yes, those are supposed to be CoreCount, not safeFuelLimit.
        var instability = 0;
        var overloadVsSizeResult = fuel - CoreCount;

        // fuel > safeFuelLimit: Slow damage. Can safely run at this level for burst periods if the engine is small and someone is keeping an eye on it.
        if (_random.Prob(0.5f))
            instability = 1;
        // overloadVsSizeResult > 5:
        if (overloadVsSizeResult > 5)
            instability = 3;
        // overloadVsSizeResult > 10: This will explode in at most 20 injections.
        if (overloadVsSizeResult > 10)
            instability = 5;

        // Apply calculated instability
        if (instability == 0)
            return powerOutput;

        overloading = true;
        var integrityCheck = 100;
        foreach (var coreUid in _cores)
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
        if (integrityCheck != 100 && _masterController.HasValue)
            _chat.SendAdminAlert($"AME overloading: {_entMan.ToPrettyString(_masterController.Value)}");

        return powerOutput;
    }

    /// <summary>
    /// Calculates the amount of power the AME can produce with the given settings
    /// </summary>
    public float CalculatePower(int fuel, int cores)
    {
        // Fuel is squared so more fuel vastly increases power and efficiency
        // We divide by the number of cores so a larger AME is less efficient at the same fuel settings
        // this results in all AMEs having the same efficiency at the same fuel-per-core setting
        return 2000000f * fuel * fuel / cores;
    }

    public int GetTotalStability()
    {
        if (CoreCount < 1)
            return 100;

        var stability = 0;
        var coreQuery = _entMan.GetEntityQuery<AmeShieldComponent>();
        foreach (var coreUid in _cores)
        {
            if (coreQuery.TryGetComponent(coreUid, out var core))
                stability += core.CoreIntegrity;
        }

        stability /= CoreCount;

        return stability;
    }

    public void ExplodeCores()
    {
        if (_cores.Count < 1
        || !_entMan.TryGetComponent<AmeControllerComponent>(MasterController, out var controller))
            return;

        /*
            * todo: add an exact to the shielding and make this find the core closest to the controller
            * so they chain explode, after helpers have been added to make it not cancer
        */
        var radius = Math.Min(2 * CoreCount * controller.InjectionAmount, 8f);
        _entMan.System<ExplosionSystem>().TriggerExplosive(MasterController.Value, radius: radius, delete: false);
    }
}
