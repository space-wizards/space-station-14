using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

public sealed partial class PlantMutateChemicalsEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantMutateChemicals>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantMutateChemicals> args)
    {
        if (entity.Comp.Seed == null)
            return;

        var chemicals = entity.Comp.Seed.Chemicals;
        var randomChems = _proto.Index(args.Effect.RandomPickBotanyReagent).Fills;

        // Add a random amount of a random chemical to this set of chemicals
        var pick = _random.Pick(randomChems);
        var chemicalId = _random.Pick(pick.Reagents);
        var amount = _random.Next(1, (int)pick.Quantity);
        var seedChemQuantity = new SeedChemQuantity();
        if (chemicals.ContainsKey(chemicalId))
        {
            seedChemQuantity.Min = chemicals[chemicalId].Min;
            seedChemQuantity.Max = chemicals[chemicalId].Max + amount;
        }
        else
        {
            seedChemQuantity.Min = 1;
            seedChemQuantity.Max = 1 + amount;
            seedChemQuantity.Inherent = false;
        }
        var potencyDivisor = (int)Math.Ceiling(100.0f / seedChemQuantity.Max);
        seedChemQuantity.PotencyDivisor = potencyDivisor;
        chemicals[chemicalId] = seedChemQuantity;
    }
}
