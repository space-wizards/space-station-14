using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly INetManager _net = default!;

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

        if (!_solutionContainer.EnsureSolution(ent.Owner,
                ent.Comp.SolutionName,
                out var solutionContainer,
                FixedPoint2.Zero))
        {
            return;
        }

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

    /// <summary>
    /// Spawns a produce item from a plant and produces the produce.
    /// </summary>
    [PublicAPI]
    public void SpawnProduce(Entity<PlantDataComponent?, PlantComponent?> ent, EntityCoordinates position)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2, false))
            return;

        // No predict RandomOffset.
        if (_net.IsClient)
            return;

        var product = _random.Pick(ent.Comp1.ProductPrototypes);
        var entity = Spawn(product, position);
        _randomHelper.RandomOffset(entity, 0.25f);

        var produce = EnsureComp<ProduceComponent>(entity);
        produce.PlantProtoId = MetaData(ent.Owner).EntityPrototype!.ID;
        produce.PlantData = ClonePlantSnapshotData(ent.Owner);
        ProduceGrown((entity, produce));
        _appearance.SetData(entity, ProduceVisuals.Potency, ent.Comp2.Potency);
    }
}
