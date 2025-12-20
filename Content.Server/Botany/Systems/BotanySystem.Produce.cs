using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;

    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        if (!TryGetPlantComponent<PlantComponent>(produce.PlantData, produce.PlantProtoId, out var plant)
            || !TryGetPlantComponent<PlantChemicalsComponent>(produce.PlantData, produce.PlantProtoId, out var chems))
            return;

        foreach (var mutation in plant.Mutations)
        {
            if (mutation.AppliesToProduce)
                _entityEffects.TryApplyEffect(uid, mutation.Effect);
        }

        if (!_solutionContainerSystem.EnsureSolution(uid,
                produce.SolutionName,
                out var solutionContainer,
                FixedPoint2.Zero))
            return;

        solutionContainer.RemoveAllSolution();

        foreach (var (chem, quantity) in chems.Chemicals)
        {
            var amount = quantity.Min;
            if (quantity.PotencyDivisor > 0 && plant.Potency > 0)
                amount += plant.Potency / quantity.PotencyDivisor;
            amount = FixedPoint2.Clamp(amount, quantity.Min, quantity.Max);
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }

    public void OnProduceExamined(EntityUid uid, ProduceComponent comp, ExaminedEvent args)
    {
        if (comp.PlantData == null
            || !TryGetPlantComponent<PlantComponent>(comp.PlantData, comp.PlantProtoId, out var plant))
            return;

        using (args.PushGroup(nameof(ProduceComponent)))
        {
            foreach (var m in plant.Mutations)
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
