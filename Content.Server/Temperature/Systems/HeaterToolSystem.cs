using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

/// <summary>
///     Allows a tool to heat things.
/// </summary>
public sealed class HeaterToolSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeaterToolComponent, AfterInteractEvent>(OnHeaterInteract);
        SubscribeLocalEvent<HeaterToolComponent, HeaterToolDoAfterEvent>(OnHeatDoAfter);
    }

    /// <summary>
    ///     Starts the heating process when a tool is used on a container.
    /// </summary>
    private void OnHeaterInteract(Entity<HeaterToolComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        var ev = new HeaterAttemptEvent(args.User);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        RaiseLocalEvent(args.Target.Value, ref ev);
        if (ev.Cancelled)
            return;

        // If frequency is 2.0, the delay is 0.5x.
        var delay = ent.Comp.DoAfterDelay / Math.Max(0.01f, ev.FrequencyMultiplier);

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
    ///     Handles adding heat and consuming fuel when the heating step completes.
    /// </summary>
    private void OnHeatDoAfter(Entity<HeaterToolComponent> ent, ref HeaterToolDoAfterEvent args)
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

        var shouldRepeat = false;

        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(args.Target.Value))
        {
            var solution = soln.Comp.Solution;
            var heatCap = solution.GetHeatCapacity(_prototype);
            if (heatCap <= 0)
                continue;

            var heatContainer = new HeatContainer(heatCap, solution.Temperature);
            
            // Heat is conducted from the tool (MaxTemperature) to the container.
            var heatToApply = heatContainer.ConductHeatQuery(ent.Comp.MaxTemperature, (float) args.Args.Delay.TotalSeconds, ent.Comp.Conductivity);

            if (heatToApply <= 0)
                continue;

            heatContainer.AddHeat(heatToApply);
            _solutionContainer.SetTemperature(soln, heatContainer.Temperature);
            shouldRepeat = true;
        }

        if (shouldRepeat)
        {
            var consumedEv = new HeaterConsumedEvent(args.User);
            RaiseLocalEvent(toolUid, ref consumedEv);
        }

        args.Repeat = shouldRepeat;
        args.Handled = true;
    }
}
