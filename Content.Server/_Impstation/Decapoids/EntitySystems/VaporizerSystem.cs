using Content.Server.Atmos.Components;
using Content.Server.Decapoids.Components;
using Content.Shared._Impstation.Decapoids;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Decapoids.EntitySystems;

public sealed partial class VaporizerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const int ExaminePriority = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VaporizerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<VaporizerComponent> ent, ref ExaminedEvent args)
    {
        switch (ent.Comp.State)
        {
            case VaporizerState.Normal:
                args.PushMarkup(Loc.GetString("vaporizer-examine-state-normal"), ExaminePriority);
                break;
            case VaporizerState.LowSolution:
                args.PushMarkup(Loc.GetString("vaporizer-examine-state-low"), ExaminePriority);
                break;
            case VaporizerState.BadSolution:
                args.PushMarkup(Loc.GetString("vaporizer-examine-state-bad"), ExaminePriority);
                break;
            case VaporizerState.Empty:
                args.PushMarkup(Loc.GetString("vaporizer-examine-state-empty"), ExaminePriority);
                break;
        }
    }

    private VaporizerState GetVaporizerState(Entity<VaporizerComponent> ent, Solution solution)
    {
        var vaporizer = ent.Comp;
        var state = VaporizerState.Empty;
        var consumeAmount = FixedPoint2.Zero;

        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != vaporizer.ExpectedReagent)
                return VaporizerState.BadSolution;

            consumeAmount += reagent.Quantity;

            if (consumeAmount / solution.MaxVolume <= vaporizer.LowPercentage)
                state = VaporizerState.LowSolution;
            else
                state = VaporizerState.Normal;
        }

        return state;
    }

    private void ProcessVaporizerTank(EntityUid uid, VaporizerComponent vaporizer, GasTankComponent gasTank, SolutionContainerManagerComponent solutionManager)
    {
        if (!_solution.TryGetSolution((uid, solutionManager), vaporizer.LiquidTank, out var solutionEnt, out var solution))
            return;

        var state = GetVaporizerState((uid, vaporizer), solution);
        vaporizer.State = state;

        if (
            gasTank.Air.Pressure < vaporizer.MaxPressure && (
                state == VaporizerState.LowSolution ||
                state == VaporizerState.Normal
            )
        )
        {
            var reagentConsumed = _solution.SplitSolution(solutionEnt.Value, vaporizer.ReagentPerSecond * vaporizer.ProcessDelay.TotalSeconds);
            gasTank.Air.AdjustMoles((int)vaporizer.OutputGas, (float)reagentConsumed.Volume * vaporizer.ReagentToMoles);
        }

        UpdateVisualState(uid, state);
    }

    private void UpdateVisualState(EntityUid uid, VaporizerState state, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;

        _appearance.SetData(uid, VaporizerVisuals.Indicator, state);
    }

    public override void Update(float frameTime)
    {
        var enumerator = EntityQueryEnumerator<VaporizerComponent, GasTankComponent, SolutionContainerManagerComponent>();

        while (enumerator.MoveNext(out var uid, out var vaporizer, out var gasTank, out var solutionManager))
        {
            if (_gameTiming.CurTime >= vaporizer.NextProcess)
            {
                ProcessVaporizerTank(uid, vaporizer, gasTank, solutionManager);
                vaporizer.NextProcess = _gameTiming.CurTime + vaporizer.ProcessDelay;
            }
        }
    }
}
