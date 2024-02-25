using Content.Server.Botany.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        if (!TryGetSeed(produce, out var seed))
            return;

        var solutionContainer = _solutionContainerSystem.EnsureSolution(uid, produce.SolutionName, FixedPoint2.Zero, out _);

        solutionContainer.RemoveAllSolution();
        foreach (var (chem, quantity) in seed.Chemicals)
        {
            var amount = FixedPoint2.Zero;
            if (seed.Potency > 0) //50 is PotencyLimit in RobustHarvest.cs
                amount += FixedPoint2.New(MathHelper.Clamp(MathHelper.Lerp(quantity.Min, quantity.Max, seed.Potency / 50), quantity.Min, quantity.Max)); // Adds from min to max amount, depending on the potency. Currently potency maxes out at 50 as per RobustHarvest.cs.
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }
}
