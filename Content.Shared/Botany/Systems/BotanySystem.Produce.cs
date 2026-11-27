using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Traits;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    /// <summary>
    /// True while <see cref="SpawnProduce"/> is spawning produce,
    /// so that harvested produce gets the traits of its plant instead of the defaults of its species.
    /// </summary>
    private bool _spawningProduce;

    [SubscribeLocalEvent]
    private void OnProduceMapInit(Entity<ProduceComponent> ent, ref MapInitEvent args)
    {
        // Harvested produce gets its traits from the harvested plant in ProduceGrown.
        // It has plant data by the time map init runs late, such as on a map that is not initialized yet.
        if (_spawningProduce || ent.Comp.PlantData != null)
            return;

        // Produce that did not come from a harvest (maps, crates, spawn menu...) gets the default traits of its species.
        if (!TryGetPlantComponent<PlantComponent>(null, ent.Comp.PlantProtoId, out var species))
            return;

        ApplyProduceTraits(ent.Owner, species.Traits);
    }

    [SubscribeLocalEvent]
    private void OnProduceExamined(Entity<ProduceComponent> ent, ref ExaminedEvent args)
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant))
            return;

        using (args.PushGroup(nameof(ProduceComponent)))
        {
            foreach (var description in _plant.GetPlantTraitDescriptions(plant))
            {
                args.PushMarkup(Loc.GetString(description));
            }
        }
    }

    private void ProduceGrown(Entity<ProduceComponent> ent)
    {
        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant)
            || !TryGetPlantComponent<PlantChemicalsComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var chems))
            return;

        ApplyProduceTraits(ent.Owner, plant.Traits);

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
    /// Applies the produce effects of each trait to a produce entity.
    /// </summary>
    private void ApplyProduceTraits(EntityUid produce, List<ProtoId<PlantTraitPrototype>> traits)
    {
        foreach (var traitId in traits)
        {
            if (!ProtoMan.TryIndex(traitId, out var trait))
                continue;

            foreach (var effect in trait.ProduceEffects)
            {
                _entityEffects.TryApplyEffect(produce, effect);
            }
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

            EntityUid entity;
            var wasSpawningProduce = _spawningProduce;
            _spawningProduce = true;
            try
            {
                entity = PredictedSpawnAtPosition(product, position);
            }
            finally
            {
                _spawningProduce = wasSpawningProduce;
            }

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
