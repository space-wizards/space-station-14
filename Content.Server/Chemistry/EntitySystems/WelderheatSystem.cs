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
/// Similar to the hotplate/SolutionHeater but as a handheld tool interaction.
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
        SubscribeLocalEvent<WelderComponent, AfterInteractEvent>(OnWelderInteract);
        SubscribeLocalEvent<WelderComponent, WelderHeatDoAfterEvent>(OnHeatDoAfter);
    }

    /// <summary>
    /// Triggered when a player clicks on an entity with a welder.
    /// Checks the welder is lit and the target has a solution, then starts the DoAfter.
    /// </summary>
    private void OnWelderInteract(EntityUid uid, WelderComponent welder, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        // Welder must be lit
        if (!_itemToggle.IsActivated(uid))
            return;

        // Target must have a solution container (beaker, bottle etc.)
        if (!TryComp<SolutionContainerManagerComponent>(args.Target, out var solutionManager))
            return;

        // Welder needs the heating component to know how much to heat
        if (!TryComp<WelderHeatComponent>(uid, out var heatComp))
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
    /// Fires each time the DoAfter completes a step.
    /// Adds a fixed chunk of heat to all solutions in the target container.
    /// Repeats automatically until max temperature is reached or welder runs out of fuel.
    /// </summary>
    private void OnHeatDoAfter(EntityUid uid, WelderComponent welder, WelderHeatDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var welderEnt = args.Used.Value;

        if (!TryComp<WelderHeatComponent>(welderEnt, out var heatComp))
            return;

        // Make sure target still has a solution
        if (!TryComp<SolutionContainerManagerComponent>(args.Target, out var solutionManager))
            return;

        // Make sure welder is still lit
        if (!_itemToggle.IsActivated(welderEnt))
            return;

        // Check and consume fuel
        if (!SolutionContainerSystem.TryGetSolution(welderEnt, welder.FuelSolutionName, out var fuelSolnComp, out var fuelSolution))
            return;

        var fuelNeeded = FixedPoint2.New(heatComp.FuelConsumptionPerHeat);
        if (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent) < fuelNeeded)
        {
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), welderEnt, args.User);
            return;
        }

        // Track whether we should keep the DoAfter looping
        var shouldRepeat = false;
        var reachedMaxTemp = false;

        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((args.Target.Value, solutionManager)))
        {
            var solution = soln.Comp.Solution;

            // Stop heating this solution if it has hit the max temperature cap
            if (solution.Temperature >= heatComp.MaxTemperature)
            {
                reachedMaxTemp = true;
                continue;
            }

            // Add the fixed heat chunk to the solution
            _solutionContainer.AddThermalEnergy(soln, heatComp.HeatPerUse);
            shouldRepeat = true;
        }

        if (reachedMaxTemp && !shouldRepeat)
        {
            _popup.PopupEntity(Loc.GetString("welder-solution-heating-max-temp"), welderEnt, args.User);
        }

        if (shouldRepeat)
        {
            // Consume fuel
            _solutionContainer.RemoveReagent(fuelSolnComp.Value, welder.FuelReagent, fuelNeeded);
        }

        // Repeat the DoAfter loop until all solutions hit max temp
        args.Repeat = shouldRepeat;
        args.Handled = true;
    }
}
