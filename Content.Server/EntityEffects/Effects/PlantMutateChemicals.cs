using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     changes the chemicals available in a plant's produce
/// </summary>
public sealed partial class PlantMutateChemicals : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var chemicals = plantholder.Seed.Chemicals;
        var _randomChems = prototypeManager.Index<WeightedRandomFillSolutionPrototype>("RandomPickBotanyReagent").Fills;


        // Add a random amount of a random chemical to this set of chemicals
        if (_randomChems != null)
        {
            var pick = random.Pick<RandomFillSolution>(_randomChems);
            string chemicalId = random.Pick(pick.Reagents);
            int amount = random.Next(1, (int)pick.Quantity);
            SeedChemQuantity seedChemQuantity = new SeedChemQuantity();
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
            int potencyDivisor = (int)Math.Ceiling(100.0f / seedChemQuantity.Max);
            seedChemQuantity.PotencyDivisor = potencyDivisor;
            chemicals[chemicalId] = seedChemQuantity;
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
