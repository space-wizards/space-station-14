using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Entity effect that mutates the chemicals of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateChemicalsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateChemicals>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateChemicals> args)
    {
        var chemicals = EnsureComp<PlantChemicalsComponent>(entity.Owner).Chemicals;
        var randomChems = _proto.Index(args.Effect.RandomPickBotanyReagent).Fills;

        // Add a random amount of a random chemical to this set of chemicals.
        var pick = _random.Pick(randomChems);
        var chemicalId = _random.Pick(pick.Reagents);
        var amount = _random.NextFloat(0.1f, (float)pick.Quantity);
        var seedChemQuantity = new PlantChemQuantity();
        if (chemicals.TryGetValue(chemicalId, out var value))
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
        chemicals[chemicalId] = seedChemQuantity;
    }
}
