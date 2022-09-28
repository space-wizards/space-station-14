using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
sealed class MaintenanceOilSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    private float _updateTimer = 0.0f;
    private const float UpdateTime = 10.0f; // 10 seconds. Can't be much lower or FixedPoint2 isn't precise enough.

    private string SolutionName = "tank";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MaintenanceOilComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;
            foreach (var component in EntityQuery<MaintenanceOilComponent>())
            {
                UpdateOil(component.Owner, component);
            }
        }
    }

    public void UpdateOil(EntityUid target, MaintenanceOilComponent? comp = null)
    {
        if (!Resolve(target, ref comp))
            return;

        BatteryDischargerComponent? substation = null;
        if (!Resolve(target, ref substation))
            return;

        PowerNetworkBatteryComponent? batteryComp = null;
        if (!Resolve(target, ref batteryComp))
            return;

        if (!_solutionContainerSystem.TryGetSolution(target, SolutionName, out var solution))
            return;

        float rate = batteryComp.NetworkBattery.CurrentSupply;

        // Use oil based on power
        FixedPoint2 amount = OilBurn(rate);
        if (_solutionContainerSystem.TryRemoveReagent(target, solution, "Oil", amount))
        {
            // Still oil left after burning, no chance of exploding.
            return;
        }

        // MTBF calculation
        var p = 0.1;
        if (_robustRandom.NextFloat() > p)
        {
            // Whew, didn't do damage
            return;
        }

        // Do damage to substation
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Structural", 10);
        _damageableSystem.TryChangeDamage(target, dspec);
    }

    /**
     * Amount (in units) of fuel to burn per update given the power flow (in Watts).
     */
    private FixedPoint2 OilBurn(float powerW)
    {
        // Exponential relationship where 20 kW -> ~50 units/hour
        return FixedPoint2.New(Math.Exp(powerW/5e3) * UpdateTime/3600);
    }

    private void OnExamine(EntityUid uid, MaintenanceOilComponent component, ExaminedEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, SolutionName, out var solution))
            return;

        var fill = solution.TotalVolume / solution.MaxVolume;
        args.PushMarkup(FillReport(fill));
    }

    public string FillReport(FixedPoint2 x)
    {
        if (x >= 0.7)
            return Loc.GetString("maintenance-oil-well");
        else if (x >= 0.4)
            return Loc.GetString("maintenance-oil-moderate");
        else if (x >= 0.1)
            return Loc.GetString("maintenance-oil-low");
        else
            return Loc.GetString("maintenance-oil-critical");
    }
}
