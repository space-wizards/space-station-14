using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Tools.Components;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
///     Allows a lit tool to heat reagents inside solution containers.
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
    ///     Starts the heating process when a tool is used on a container.
    /// </summary>
    private void OnWelderInteract(Entity<WelderHeatComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        if (!_itemToggle.IsActivated(ent.Owner))
            return;

        var ev = new HeatableAttemptEvent(args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);
        if (ev.Cancelled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterDelay,
            new WelderHeatDoAfterEvent(), ent,
            target: args.Target,
            used: ent)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    /// <summary>
    ///     Handles adding heat and consuming fuel when the heating step completes.
    /// </summary>
    private void OnHeatDoAfter(Entity<WelderHeatComponent> ent, ref WelderHeatDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var welderEnt = args.Used.Value;

        if (!TryComp<WelderComponent>(welderEnt, out var welder))
            return;

        if (!_itemToggle.IsActivated(welderEnt))
            return;

        if (!_solutionContainer.TryGetSolution(welderEnt, welder.FuelSolutionName, out var fuelSolnComp, out var fuelSolution))
            return;

        var fuelNeeded = FixedPoint2.New(ent.Comp.FuelConsumptionPerHeat);
        if (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent) < fuelNeeded)
        {
            _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), welderEnt, args.User);
            return;
        }

        var shouldRepeat = false;
        var reachedMaxTemp = false;

        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(args.Target.Value))
        {
            var solution = soln.Comp.Solution;

            if (solution.Temperature >= ent.Comp.MaxTemperature)
            {
                reachedMaxTemp = true;
                continue;
            }

            var heatContainer = new HeatContainer(solution.GetHeatCapacity(null), solution.Temperature);
            heatContainer.AddHeat(ent.Comp.HeatPerUse * (float) args.Args.Delay.TotalSeconds);
            _solutionContainer.SetTemperature(soln, heatContainer.Temperature);
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
