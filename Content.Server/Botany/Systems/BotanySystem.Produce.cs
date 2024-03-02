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
            var amount = FixedPoint2.New(quantity.Min);
            if (seed.Potency > 0)
            {
                // Adds an amount, depending on the potency. The PotencyLimit in RobustHarvest is 50,
                // but mutations (MutationSystem.cs) can cause it to go over PotencyLimit, up to 100.
                amount += FixedPoint2.New(MathHelper.Lerp(0, quantity.PotencyAddend, seed.Potency / 50));
            }
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }
}
