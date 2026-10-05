using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

/// <summary>
///     Allows a tool to directly heat solutions inside containers.
/// </summary>
public sealed partial class HeaterToolSystem : EntitySystem
{
    private const float MinFrequencyMultiplier = 0.01f;

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeaterToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HeaterToolComponent, HeaterToolDoAfterEvent>(OnHeaterToolDoAfter);
    }

    /// <summary>
    ///     Starts the heating process when a tool is used on a container.
    /// </summary>
    private void OnAfterInteract(Entity<HeaterToolComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        // Only interact with entities that actually contain solutions with heat capacity.
        if (!TryComp<SolutionContainerManagerComponent>(args.Target.Value, out var container))
            return;

        var hasSolution = false;
        var canHeat = false;
        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((args.Target.Value, container)))
        {
            var solution = soln.Comp.Solution;
            if (solution.GetHeatCapacity(_prototype) <= 0)
                continue;

            hasSolution = true;
            if (solution.Temperature < ent.Comp.MaxTemperature)
            {
                canHeat = true;
                break;
            }
        }

        if (!hasSolution)
            return;

        var ev = new HeaterAttemptEvent(args.User);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        RaiseLocalEvent(args.Target.Value, ref ev);
        if (ev.Cancelled)
            return;

        if (!canHeat)
        {
            _popup.PopupEntity(Loc.GetString("welder-solution-heating-max-temp"), args.Target.Value, args.User);
            args.Handled = true;
            return;
        }

        // If frequency is 2.0, the delay is 0.5x.
        var delay = ent.Comp.DoAfterDelay / Math.Max(MinFrequencyMultiplier, ev.FrequencyMultiplier);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay,
            new HeaterToolDoAfterEvent(), ent,
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
    ///     Handles conducting heat and consuming resources when the heating do-after completes.
    /// </summary>
    private void OnHeaterToolDoAfter(Entity<HeaterToolComponent> ent, ref HeaterToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var toolUid = args.Used.Value;

        var ev = new HeaterAttemptEvent(args.User);
        RaiseLocalEvent(toolUid, ref ev);
        if (ev.Cancelled)
            return;

        RaiseLocalEvent(args.Target.Value, ref ev);
        if (ev.Cancelled)
            return;

        var didHeat = false;
        var canStillHeat = false;

        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(args.Target.Value))
        {
            var solution = soln.Comp.Solution;
            var heatCap = solution.GetHeatCapacity(_prototype);
            if (heatCap <= 0)
                continue;

            var heatContainer = new HeatContainer(heatCap, solution.Temperature);

            // Conduct heat from the tool's flame to the solution container using foundational ConductHeat
            var heatToApply = heatContainer.ConductHeat(ent.Comp.MaxTemperature, (float) args.Args.Delay.TotalSeconds, ent.Comp.Conductivity);

            if (heatToApply <= 0f)
                continue;

            _solutionContainer.SetTemperature(soln, heatContainer.Temperature);
            didHeat = true;

            if (heatContainer.Temperature < ent.Comp.MaxTemperature)
                canStillHeat = true;
        }

        if (didHeat)
        {
            var consumedEv = new HeaterConsumedEvent(args.User);
            RaiseLocalEvent(toolUid, ref consumedEv);

            if (!canStillHeat)
                _popup.PopupEntity(Loc.GetString("welder-solution-heating-max-temp"), args.Target.Value, args.User);
        }

        args.Repeat = didHeat && canStillHeat;
        args.Handled = true;
    }
}
