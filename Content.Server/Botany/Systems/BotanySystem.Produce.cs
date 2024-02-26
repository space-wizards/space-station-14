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
            if (seed.Potency > 0)
            {
                amount += FixedPoint2.New(MathHelper.Lerp(quantity.Min, quantity.Max, seed.Potency / 50));
                // Adds an amount, depending on the potency. The maximum is 50 as per RobustHarvest.cs,
                // but mutations (MutationSystem.cs) can cause it to go over the max, up to 100.
            }
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }
}
