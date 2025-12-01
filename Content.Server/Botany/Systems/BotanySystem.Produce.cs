using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        if (!TryGetSeed(produce, out var seed))
            return;

        foreach (var mutation in seed.Mutations)
        {
            if (mutation.AppliesToProduce)
            {
                var args = new EntityEffectBaseArgs(uid, EntityManager);
                mutation.Effect.Effect(args);
            }
        }

        if (!_solutionContainerSystem.EnsureSolution(uid,
                produce.SolutionName,
                out var solutionContainer,
                FixedPoint2.Zero))
            return;

        solutionContainer.RemoveAllSolution();
        foreach (var (chem, quantity) in seed.Chemicals)
        {
            var amount = FixedPoint2.New(quantity.Min);
            if (quantity.PotencyDivisor > 0 && seed.Potency > 0)
                amount += FixedPoint2.New(seed.Potency / quantity.PotencyDivisor);
            amount = FixedPoint2.New(MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }

    public void OnProduceExamined(EntityUid uid, ProduceComponent comp, ExaminedEvent args)
    {
        if (comp.Seed == null)
            return;

        using (args.PushGroup(nameof(ProduceComponent)))
        {
            foreach (var m in comp.Seed.Mutations)
            {
                // Don't show mutations that have no effect on produce (sentience)
                if (!m.AppliesToProduce)
                    continue;

                if (m.Description != null)
                    args.PushMarkup(Loc.GetString(m.Description));
            }
        }
    }
}
