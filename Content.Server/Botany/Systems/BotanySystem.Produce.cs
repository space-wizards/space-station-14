using Content.Server.Botany.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        if (!TryGetSeed(produce, out var seed))
            return;

        if (TryComp(uid, out SpriteComponent? sprite))
        {
            sprite.LayerSetRSI(0, seed.PlantRsi);
            sprite.LayerSetState(0, seed.PlantIconState);
        }

        Solution.ReagentQuantity[] reagents = new Solution.ReagentQuantity[seed.Chemicals.Count];
        int i = 0;
        foreach (var (chem, quantity) in seed.Chemicals)
        {
            var amount = FixedPoint2.New(quantity.Min);
            if (quantity.PotencyDivisor > 0 && seed.Potency > 0)
                amount += FixedPoint2.New(seed.Potency / quantity.PotencyDivisor);
            amount = FixedPoint2.New((int) MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
            reagents[i++] = new(chem, amount);
        }

        _solutionContainerSystem.EnsureSolution(uid, produce.SolutionName, reagents);
    }
}
