using Content.Server.Botany.Components;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        SeedPrototype? seed;
        // try get seed from seed database
        if (produce.SeedUid == null || !Seeds.TryGetValue(produce.SeedUid.Value, out seed))
        {
            // try get seed from base prototype
            if (produce.SeedName == null || !_prototypeManager.TryIndex(produce.SeedName, out seed))
                return;
        }

        if (TryComp(uid, out SpriteComponent? sprite))
        {
            sprite.LayerSetRSI(0, seed.PlantRsi);
            sprite.LayerSetState(0, seed.PlantIconState);
        }

        var solutionContainer = _solutionContainerSystem.EnsureSolution(uid, produce.SolutionName);

        solutionContainer.RemoveAllSolution();
        foreach (var (chem, quantity) in seed.Chemicals)
        {
            var amount = FixedPoint2.New(quantity.Min);
            if (quantity.PotencyDivisor > 0 && seed.Potency > 0)
                amount += FixedPoint2.New(seed.Potency / quantity.PotencyDivisor);
            amount = FixedPoint2.New((int) MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }
}
