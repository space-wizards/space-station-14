using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Components;

namespace Content.Shared.Tools.Systems;

public abstract partial class SharedToolSystem
{
    public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(
        EntityUid uid,
        WelderComponent? welder = null,
        SolutionContainerManagerComponent? solutionContainer = null)
    {
        if (!Resolve(uid, ref welder, ref solutionContainer))
            return default;

        if (!SolutionContainer.TryGetSolution(
                (uid, solutionContainer),
                welder.FuelSolutionName,
                out _,
                out var fuelSolution))
        {
            return default;
        }

        return (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent), fuelSolution.MaxVolume);
    }
}
