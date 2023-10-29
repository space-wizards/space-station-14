using Content.Server.Ame.Components;
using Content.Server.Chat.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ame.EntitySystems;

public sealed partial class AmeSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly AmeControllerSystem _controllerSystem = default!;
    [Dependency] private readonly AmeShieldingSystem _shieldingSystem = default!;
    private EntityQuery<AmeControllerComponent> _controllerQuery = default!;
    private EntityQuery<AmeShieldComponent> _shieldQuery = default!;

    /// <summary>
    /// The amount of watts an AME produces per unit of injected fuel scaled by fuel/core.
    /// </summary>
    public const float BaseAmePower = 20000f;


    public override void Initialize()
    {
        base.Initialize();

        _controllerQuery = GetEntityQuery<AmeControllerComponent>();
        _shieldQuery = GetEntityQuery<AmeShieldComponent>();
    }

    public void AddCore(EntityUid ameId, EntityUid coreId, AmeComponent? ame = null)
    {
        if (!Resolve(ameId, ref ame))
            return;

        ame.Cores.Add(coreId);

        UpdateVisuals(ameId, ame);
    }

    public void RemoveCore(EntityUid ameId, EntityUid coreId, AmeComponent? ame = null)
    {
        if (!Resolve(ameId, ref ame))
            return;

        ame.Cores.Remove(coreId);

        _shieldingSystem.UpdateVisuals(coreId, 0, false);
        UpdateVisuals(ameId, ame);
    }

    public void SetMasterController(EntityUid ameId, EntityUid? value, AmeComponent? ame = null)
    {
        if (!Resolve(ameId, ref ame))
            return;

        if (value == ame.MasterController)
            return;

        ame.MasterController = value;

        UpdateVisuals(ameId, ame);
    }


    /// <summary>
    /// 
    /// </summary>
    public float InjectFuel(EntityUid ameId, int fuel, out bool overloading, AmeComponent? ame = null)
    {
        overloading = false;
        if (!Resolve(ameId, ref ame))
            return 0;

        var coreCount = ame.Cores.Count;
        if (fuel <= 0 || coreCount <= 0)
            return 0;

        var safeFuelLimit = coreCount * 2;

        var powerOutput = CalculatePower(fuel, coreCount);
        if (fuel <= safeFuelLimit)
            return powerOutput;

        // The AME is being overloaded.
        // Note about these maths: I would assume the general idea here is to make larger engines less safe to overload.
        // In other words, yes, those are supposed to be CoreCount, not safeFuelLimit.
        var instability = 0;
        var overloadVsSizeResult = fuel - coreCount;

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
        foreach (var coreUid in ame.Cores)
        {
            if (!_shieldQuery.TryGetComponent(coreUid, out var core))
                continue;

            var oldIntegrity = core.CoreIntegrity;
            core.CoreIntegrity -= instability;

            if (oldIntegrity > 95
                && core.CoreIntegrity <= 95
                && core.CoreIntegrity < integrityCheck)
                integrityCheck = core.CoreIntegrity;
        }

        // Admin alert
        if (integrityCheck != 100)
            _chatMan.SendAdminAlert($"AME overloading: {ToPrettyString(ameId)}");

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
        return BaseAmePower * fuel * fuel / cores;
    }

    /// <summary>
    /// 
    /// </summary>
    public int GetTotalStability(EntityUid ameId, AmeComponent? ame = null)
    {
        if (!Resolve(ameId, ref ame))
            return 100;

        if (ame.Cores.Count < 1)
            return 100;

        var stability = 0;
        foreach (var coreUid in ame.Cores)
        {
            if (_shieldQuery.TryGetComponent(coreUid, out var core))
                stability += core.CoreIntegrity;
        }

        stability /= ame.Cores.Count;

        return stability;
    }

    /// <summary>
    /// 
    /// </summary>
    public void ExplodeCores(EntityUid ameId, AmeComponent? ame = null)
    {
        if (!Resolve(ameId, ref ame))
            return;

        if (ame.MasterController is not { } controllerId || !TryComp<AmeControllerComponent>(controllerId, out var controller))
            return;

        /*
            * todo: add an exact to the shielding and make this find the core closest to the controller
            * so they chain explode, after helpers have been added to make it not cancer
        */
        // Note for the future. AME fuel jars currently operate at around 400kJ/unit of antimatter.
        // This gives each unit of antimatter around 1/5 the energy of a C4 demolition charge (~500g/charge).
        // If we assume that the syndie C4 charge is similar to the IRL version at 60 total intensity this gives 12 intensity/antimatter unit.
        var radius = Math.Min(2 * ame.Cores.Count * controller.InjectionAmount, 8f);
        _explosionSystem.TriggerExplosive(controllerId, radius: radius, delete: false);
    }

    /// <summary>
    /// 
    /// </summary>
    public void UpdateVisuals(EntityUid ameId, AmeComponent? ame = null)
    {
        if (!Resolve(ameId, ref ame))
            return;

        var injectionAmt = 0;
        var injecting = false;

        if (TryComp<AmeControllerComponent>(ame.MasterController, out var controller))
        {
            injectionAmt = controller.InjectionAmount;
            injecting = controller.Injecting;
        }

        var injectionRatio = ame.Cores.Count > 0 ? injectionAmt / ame.Cores.Count : 0;
        foreach (var coreId in ame.Cores)
            _shieldingSystem.UpdateVisuals(coreId, injectionRatio, injecting);

        foreach (var nodeId in Comp<NodeGraphComponent>(ameId).Nodes)
        {
            if (TryComp<AmeControllerComponent>(nodeId, out controller))
                _controllerSystem.UpdateUi(nodeId, controller);
        }
    }
}
