using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles the chemicals of a plant.
/// </summary>
public sealed class PlantChemicalsSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantChemicalsComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
    }

    private void OnCrossPollinate(Entity<PlantChemicalsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossChemicals(ref ent.Comp.Chemicals, pollenData.Chemicals);
    }

    /// <summary>
    /// Adds a random chemical to the plant chemicals.
    /// </summary>
    [PublicAPI]
    public void MutateRandomChemical(Entity<PlantChemicalsComponent?> ent, List<RandomFillSolution> randomChems)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var pick = rand.Pick(randomChems);
        var chemicalId = rand.Pick(pick.Reagents);
        var amount = rand.NextFloat(0.1f, (float)pick.Quantity);
        var seedChemQuantity = new PlantChemQuantity();
        if (ent.Comp.Chemicals.TryGetValue(chemicalId, out var value))
        {
            seedChemQuantity.Min = value.Min;
            seedChemQuantity.Max = value.Max + amount;
        }
        else
        {
            seedChemQuantity.Min = FixedPoint2.Epsilon;
            seedChemQuantity.Max = FixedPoint2.Zero + amount;
            seedChemQuantity.Inherent = false;
        }

        var potencyDivisor = 100f / seedChemQuantity.Max;
        seedChemQuantity.PotencyDivisor = (float)potencyDivisor;
        ent.Comp.Chemicals[chemicalId] = seedChemQuantity;
        Dirty(ent);
    }
}
