using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// Allows a lit welder to heat reagents inside solution containers.
/// </summary>
public sealed class WelderHeatSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WelderHeatComponent, AfterInteractEvent>(OnWelderInteract);
        SubscribeLocalEvent<WelderHeatComponent, WelderHeatDoAfterEvent>(OnHeatDoAfter);
    }

    /// <summary>
    /// Starts the heating process when a welder is used on a container.
    /// </summary>
    private void OnWelderInteract(EntityUid uid, WelderHeatComponent heatComp, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        if (!_itemToggle.IsActivated(uid))
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Target, out var solutionManager))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, heatComp.DoAfterDelay,
            new WelderHeatDoAfterEvent(), uid,
            target: args.Target,
            used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    /// <summary>
    /// Handles adding heat and consuming fuel when the heating step completes.
    /// </summary>
    private void OnHeatDoAfter(EntityUid uid, WelderHeatComponent heatComp, WelderHeatDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var welderEnt = args.Used.Value;

        if (!TryComp<WelderComponent>(welderEnt, out var welder))
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Target, out var solutionManager))
            return;

        if (!_itemToggle.IsActivated(welderEnt))
            return;

        if (!_solutionContainer.TryGetSolution(welderEnt, welder.FuelSolutionName, out var fuelSolnComp, out var fuelSolution))
            return;

        var fuelNeeded = FixedPoint2.New(heatComp.FuelConsumptionPerHeat);
        if (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent) < fuelNeeded)
        {
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), welderEnt, args.User);
            return;
        }

        var shouldRepeat = false;
        var reachedMaxTemp = false;

        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((args.Target.Value, solutionManager)))
        {
            var solution = soln.Comp.Solution;

            if (solution.Temperature >= heatComp.MaxTemperature)
            {
                reachedMaxTemp = true;
                continue;
            }

            _solutionContainer.AddThermalEnergy(soln, heatComp.HeatPerUse);
            shouldRepeat = true;
        }

        if (reachedMaxTemp && !shouldRepeat)
        {
            _popup.PopupEntity(Loc.GetString("welder-solution-heating-max-temp"), welderEnt, args.User);
        }

        if (shouldRepeat)
        {
            _solutionContainer.RemoveReagent(fuelSolnComp.Value, welder.FuelReagent, fuelNeeded);
        }

        args.Repeat = shouldRepeat;
        args.Handled = true;
    }
}
