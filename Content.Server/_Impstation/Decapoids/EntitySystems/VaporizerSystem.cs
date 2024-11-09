using Content.Server.Atmos.Components;
using Content.Server.Decapoids.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Timing;

namespace Content.Server.Decapoids.EntitySystems;

public sealed partial class VaporizerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void ProcessVaporizerTank(EntityUid uid, VaporizerComponent vaporizer, GasTankComponent gasTank, SolutionContainerManagerComponent solutionManager)
    {
        if (gasTank.Air.Pressure >= vaporizer.MaxPressure)
            return;

        if (!_solution.TryGetSolution((uid, solutionManager), vaporizer.LiquidTank, out var ent, out var solution))
            return;

        // Validate solution
        var valid = true;
        ReagentQuantity? consumeReagent = null;
        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != vaporizer.ExpectedReagent)
            {
                valid = false;
                break;
            }
            consumeReagent ??= reagent;
        }
        if (!valid || !consumeReagent.HasValue)
            return;

        var reagentConsumed = solution.RemoveReagent(new ReagentQuantity(consumeReagent.Value.Reagent, vaporizer.ReagentPerSecond * vaporizer.ProcessDelay.TotalSeconds));
        gasTank.Air.AdjustMoles((int)vaporizer.OutputGas, (float)reagentConsumed * vaporizer.ReagentToMoles);
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
