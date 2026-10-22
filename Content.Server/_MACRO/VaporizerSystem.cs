using Content.Shared._MACRO.Decapoids;
using Content.Shared._MACRO.Decapoids.Components;
using Content.Shared._MACRO.Decapoids.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server._MACRO.Decapoids.EntitySystems;

public sealed partial class VaporizerSystem : SharedVaporizerSystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void AdjustTankMoles(VaporizerComponent vaporizer, GasTankComponent gasTank, Entity<SolutionComponent> solution)
    {
        // Split off the reagents consumed
        var reagentConsumed = _solution.SplitSolution(
            solution,
            vaporizer.ReagentPerSecond * vaporizer.ProcessDelay.TotalSeconds);
        // Add gas to the gas tank
        gasTank.Air.AdjustMoles(vaporizer.OutputGas, (float)reagentConsumed.Volume * vaporizer.ReagentToMoles);
    }
}
