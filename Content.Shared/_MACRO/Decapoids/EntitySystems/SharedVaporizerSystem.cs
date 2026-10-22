using Content.Shared._MACRO.Decapoids.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._MACRO.Decapoids.EntitySystems;

public abstract partial class SharedVaporizerSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private const int ExaminePriority = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VaporizerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<VaporizerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("vaporizer-examine-state",("state", ent.Comp.State)), ExaminePriority);
    }

    /// <summary>
    /// Convert a portion of reagent inside of the vaporizer to gas.
    /// </summary>
    /// <param name="ent">Vaporizer to process.</param>
    /// <param name="gasTank">Gas Tank component to add to.</param>
    /// <param name="solutionManager">Solution Manager to get the solution from.</param>
    private void ProcessVaporizerTank(Entity<VaporizerComponent> ent, GasTankComponent gasTank)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.LiquidTank, out var solutionEnt, out var solution))
            return;

        var state = GetVaporizerState(ent, solution);

        if (ent.Comp.State != state)
        {
            ent.Comp.State = state;
            UpdateVisualState(ent, state);
            Dirty(ent);
        }

        // If the air pressure isn't less than max AND the state is low or normal, return.
        if (gasTank.Air.Pressure >= ent.Comp.MaxPressure ||
            state is not (VaporizerState.LowSolution or VaporizerState.Normal))
            return;

        AdjustTankMoles(ent.Comp, gasTank, solutionEnt.Value);
        Dirty(ent, gasTank);
    }

    /// <summary>
    /// Adjusts gas tank mols SERVERSIDE in order to not eat bandwidth.
    /// </summary>
    /// <param name="vaporizer">Vaporizer component</param>
    /// <param name="gasTank">Gas tank component.</param>
    /// <param name="solution">Solution to consume reagents from.</param>
    public virtual void AdjustTankMoles(VaporizerComponent vaporizer, GasTankComponent gasTank, Entity<SolutionComponent> solution) { }

    /// <summary>
    /// Get the fill state of a vaporizer's solution.
    /// </summary>
    /// <param name="ent">Vaporizer entity</param>
    /// <param name="solution">Solution to get fill level of.</param>
    /// <returns></returns>
    private static VaporizerState GetVaporizerState(Entity<VaporizerComponent> ent, Solution solution)
    {
        var vaporizer = ent.Comp;
        var state = VaporizerState.Empty;
        var consumeAmount = FixedPoint2.Zero;

        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != vaporizer.ExpectedReagent)
                return VaporizerState.BadSolution;

            consumeAmount += reagent.Quantity;

            state = consumeAmount / solution.MaxVolume <= vaporizer.LowPercentage
                ? VaporizerState.LowSolution
                : VaporizerState.Normal;
        }

        return state;
    }

    private void UpdateVisualState(EntityUid uid, VaporizerState state, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;

        _appearance.SetData(uid, VaporizerVisuals.Indicator, state);
    }

    public override void Update(float frameTime)
    {
        var enumerator = EntityQueryEnumerator<VaporizerComponent, GasTankComponent>();

        while (enumerator.MoveNext(out var uid, out var vaporizer, out var gasTank))
        {
            if (_gameTiming.CurTime < vaporizer.NextProcess)
                continue;

            ProcessVaporizerTank((uid, vaporizer), gasTank);
            vaporizer.NextProcess = _gameTiming.CurTime + vaporizer.ProcessDelay;
        }
    }
}
