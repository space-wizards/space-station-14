using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem
{
    private void ProduceGrown(Entity<ProduceComponent> ent)
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant)
            || !TryGetPlantComponent<PlantChemicalsComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var chems))
            return;

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
    public void SpawnProduce(
        Entity<PlantComponent?, PlantDataComponent?> ent,
        EntityCoordinates position,
        int amount)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        for (var i = 0; i < amount; i++)
        {
            var product = random.Pick(ent.Comp2.ProductPrototypes);
            var entity = PredictedSpawnAtPosition(product, position);
            _randomHelper.RandomOffset(entity, 0.25f, random);

            var produce = EnsureComp<ProduceComponent>(entity);
            produce.PlantProtoId = MetaData(ent.Owner).EntityPrototype!.ID;
            produce.PlantData = ClonePlantSnapshotData(ent.Owner, parent: entity);
            Dirty(entity, produce);
            ProduceGrown((entity, produce));
            _appearance.SetData(entity, ProduceVisuals.Potency, ent.Comp1.Potency);

            var ev = new PlantProduceSpawnedEvent(entity);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }
}
