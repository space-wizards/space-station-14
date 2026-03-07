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

        var heaterAttempt = new HeaterAttemptEvent(args.User);
        RaiseLocalEvent(ent, ref heaterAttempt);
        if (heaterAttempt.Cancelled)
            return;

        var heatableAttempt = new HeatableAttemptEvent(args.User);
        RaiseLocalEvent(args.Target.Value, ref heatableAttempt);
        if (heatableAttempt.Cancelled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterDelay,
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

        var heaterAttempt = new HeaterAttemptEvent(args.User);
        RaiseLocalEvent(toolUid, ref heaterAttempt);
        if (heaterAttempt.Cancelled)
            return;

        var shouldRepeat = false;

        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(args.Target.Value))
        {
            var solution = soln.Comp.Solution;
            var heatCap = solution.GetHeatCapacity(_prototype);
            if (heatCap <= 0)
                continue;

            var heatContainer = new HeatContainer(heatCap, solution.Temperature);
            var energy = ent.Comp.HeatPerUse * (float) args.Args.Delay.TotalSeconds;

            // Limit heat to max temperature
            var dQ = heatContainer.ConductHeatToTempQuery(ent.Comp.MaxTemperature);
            var heatToApply = Math.Min(energy, Math.Max(0, dQ));

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
