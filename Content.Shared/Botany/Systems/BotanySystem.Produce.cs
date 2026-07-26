using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [SubscribeLocalEvent]
    private void OnProduceExamined(Entity<ProduceComponent> ent, ref ExaminedEvent args)
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant))
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

    private void ProduceGrown(Entity<ProduceComponent> ent)
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant)
            || !TryGetPlantComponent<PlantChemicalsComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var chems))
            return;

        foreach (var mutation in plant.Mutations)
        {
            if (mutation.AppliesToProduce)
                _entityEffects.TryApplyEffect(ent.Owner, mutation.Effect);
        }

        _solutionContainer.EnsureSolution(ent.Owner, ent.Comp.TargetSolution, out var solution);
        solution.Comp.Solution.RemoveAllSolution();

        foreach (var (chem, quantity) in chems.Chemicals)
        {
            var amount = quantity.Min;
            if (quantity.PotencyDivisor > 0 && plant.Potency > 0)
                amount += plant.Potency / quantity.PotencyDivisor;
            amount = FixedPoint2.Clamp(amount, quantity.Min, quantity.Max);
            solution.Comp.Solution.MaxVolume += amount;
            solution.Comp.Solution.AddReagent(chem, amount);
        }
    }

    /// <summary>
    /// Spawns a produce item from a plant and produces the produce.
    /// </summary>
    [PublicAPI]
    public void SpawnProduce(Entity<PlantComponent?> ent, EntityCoordinates position)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var product = random.Pick(ent.Comp.ProductPrototypes);
        var entity = PredictedSpawnAtPosition(product, position);
        _randomHelper.RandomOffset(entity, 0.25f, random);

        var produce = EnsureComp<ProduceComponent>(entity);
        produce.PlantProtoId = MetaData(ent.Owner).EntityPrototype!.ID;
        produce.PlantData = ClonePlantSnapshotData(ent.Owner, parent: entity);
        Dirty(entity, produce);
        ProduceGrown((entity, produce));
        _appearance.SetData(entity, ProduceVisuals.Potency, ent.Comp.Potency);
    }
}
