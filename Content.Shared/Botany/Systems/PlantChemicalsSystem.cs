using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles the chemicals of a plant.
/// </summary>
public sealed partial class PlantChemicalsSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantChemicalsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossChemicals(ent, ref ent.Comp.Chemicals, pollenData.Chemicals);
        Dirty(ent);
    }

    /// <summary>
    /// Adds a chemical to a plant.
    /// </summary>
    [PublicAPI]
    public bool AddChemical(
        Entity<PlantChemicalsComponent?> ent,
        ProtoId<ReagentPrototype> chemicalId,
        FixedPoint2 min,
        FixedPoint2 amount,
        bool inherent = false)
    {
        if (!Resolve(ent, ref ent.Comp, false) || min < 0 || amount < 0 || min + amount <= 0)
            return false;

        var seedChemQuantity = new PlantChemQuantity
        {
            Inherent = inherent //Overwrite inherent property with incoming chem value
        };

        if (TryGetChemical(ent, chemicalId, out var value))
        {
            // If the value already exists, take the max of the two mins,
            // then add the amount to the old max
            seedChemQuantity.Min = FixedPoint2.Max(value.Min, min);
            seedChemQuantity.Max = value.Max + amount;
        }
        else
        {
            //Otherwise initialize it with min and max as min + amount
            seedChemQuantity.Min = min;
            seedChemQuantity.Max = min + amount;
        }
        var potencyDivisor = 100f / seedChemQuantity.Max;
        seedChemQuantity.PotencyDivisor = (float)potencyDivisor;

        ent.Comp.Chemicals[chemicalId] = seedChemQuantity;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Gets the quantity for a chemical on a plant.
    /// </summary>
    [PublicAPI]
    public bool TryGetChemical(
        Entity<PlantChemicalsComponent?> ent,
        ProtoId<ReagentPrototype> chemicalId,
        out PlantChemQuantity quantity)
    {
        quantity = default;
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.Chemicals.TryGetValue(chemicalId, out quantity);
    }

    /// <summary>
    /// Adds a random chemical to the plant chemicals.
    /// </summary>
    [PublicAPI]
    public void MutateRandomChemical(Entity<PlantChemicalsComponent?> ent, IReadOnlyList<ProtoId<WeightedRandomFillSolutionPrototype>> randomChemTables)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var totalWeight = 0f;
        foreach (var table in randomChemTables)
        {
            foreach (var fill in ProtoMan.Index(table).Fills)
            {
                totalWeight += fill.Weight;
            }
        }

        if (totalWeight <= 0f)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var selectedWeight = random.NextFloat() * totalWeight;
        var accumulatedWeight = 0f;
        WeightedRandomFillSolutionPrototype? selectedTable = null;

        foreach (var table in randomChemTables)
        {
            var tableProto = ProtoMan.Index(table);
            foreach (var fill in tableProto.Fills)
            {
                accumulatedWeight += fill.Weight;
            }

            if (accumulatedWeight > selectedWeight)
            {
                selectedTable = tableProto;
                break;
            }
        }

        if (selectedTable == null)
            return;

        var (chemicalId, quantity) = selectedTable.Pick(random);

        var amount = FixedPoint2.Max(random.NextFloat(0f, 1f) * quantity, FixedPoint2.Epsilon);
        AddChemical(
            ent: ent,
            chemicalId: chemicalId,
            min: FixedPoint2.Clamp(quantity / 5f, FixedPoint2.Epsilon, 1f), //Set the minimum to a fifth of the quantity to give some level of bad luck protection

            amount: amount,
            inherent: false
        );

        Dirty(ent);
    }
}
