using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;

namespace Content.Server.Botany;

public sealed class MutationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private WeightedRandomFillSolutionPrototype _randomChems = default!;
    private RandomPlantMutationListPrototype _randomMutations = default!;

    //Additonal TODO:
    //clean up errors on client side about missing concrete Glow class?

    //Remaining mutations to port:
    //remaining fun (4)
    // - Seedless, Ligneous, turnintokudzu are plant traits rather than effects and their value should stay in SeedData for now
    // - Screaming will need updated with new screams from more recent commit.
    //harvest type and autoharvest (2)
    //gases (2, eat/make)
    //chems (1)

    public override void Initialize()
    {
        _randomChems = _prototypeManager.Index<WeightedRandomFillSolutionPrototype>("RandomPickBotanyReagent");
        _randomMutations = _prototypeManager.Index<RandomPlantMutationListPrototype>("RandomPlantMutations");
    }

    /// <summary>
    /// For each random mutation, see if it occurs on this plant this check.
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="severity"></param>
    public void CheckRandomMutations(EntityUid plantHolder, ref SeedData seed, float severity)
    {
        foreach (var mutation in _randomMutations.mutations)
        {
            if (Random(mutation.BaseOdds * severity))
            {
                if (mutation.AppliesToPlant)
                { 
                    EntityEffectBaseArgs args = new EntityEffectBaseArgs(plantHolder, EntityManager);
                    mutation.Mutation.Effect(args);
                }
                if (mutation.Persists) //Stat adjustments do not persist by being an attached effect, they just change the stat.
                    seed.Mutations.Add(mutation);
            }
        }
    }

    /// <summary>
    /// Main idea: Simulate genetic mutation using random binary flips.  Each
    /// seed attribute can be encoded with a variable number of bits, e.g.
    /// NutrientConsumption is represented by 5 bits randomly distributed in the
    /// plant's genome which thermometer code the floating value between 0.1 and
    /// 5. 1 unit of mutation flips one bit in the plant's genome, which changes
    /// NutrientConsumption if one of those 5 bits gets affected.
    ///
    /// You MUST clone() seed before mutating it!
    /// </summary>
    public void MutateSeed(EntityUid plantHolder, ref SeedData seed, float severity)
    {
        if (!seed.Unique)
        {
            Log.Error($"Attempted to mutate a shared seed");
            return;
        }

        CheckRandomMutations(plantHolder, ref seed, severity); //TODO Will be the main call later, just check if this runs for now.

        // Add up everything in the bits column and put the number here.
        const int totalbits = 262;

        #pragma warning disable IDE0055 // disable formatting warnings because this looks more readable
        // Fun (72)
        MutateBool(ref seed.Seedless      , true , 10, totalbits, severity);
        MutateBool(ref seed.Ligneous      , true , 10, totalbits, severity);
        MutateBool(ref seed.TurnIntoKudzu , true , 10, totalbits, severity);
        MutateBool(ref seed.CanScream     , true , 10, totalbits, severity);
        #pragma warning restore IDE0055

        // ConstantUpgade (10)
        MutateHarvestType(ref seed.HarvestRepeat, 10, totalbits, severity);

        // Gas (5)
        MutateGasses(ref seed.ExudeGasses, 0.01f, 0.5f, 4, totalbits, severity);
        MutateGasses(ref seed.ConsumeGasses, 0.01f, 0.5f, 1, totalbits, severity);

        // Chems (20)
        MutateChemicals(ref seed.Chemicals, 20, totalbits, severity);
    }

    public SeedData Cross(SeedData a, SeedData b)
    {
        SeedData result = b.Clone();

        CrossChemicals(ref result.Chemicals, a.Chemicals);

        CrossFloat(ref result.NutrientConsumption, a.NutrientConsumption);
        CrossFloat(ref result.WaterConsumption, a.WaterConsumption);
        CrossFloat(ref result.IdealHeat, a.IdealHeat);
        CrossFloat(ref result.HeatTolerance, a.HeatTolerance);
        CrossFloat(ref result.IdealLight, a.IdealLight);
        CrossFloat(ref result.LightTolerance, a.LightTolerance);
        CrossFloat(ref result.ToxinsTolerance, a.ToxinsTolerance);
        CrossFloat(ref result.LowPressureTolerance, a.LowPressureTolerance);
        CrossFloat(ref result.HighPressureTolerance, a.HighPressureTolerance);
        CrossFloat(ref result.PestTolerance, a.PestTolerance);
        CrossFloat(ref result.WeedTolerance, a.WeedTolerance);

        CrossFloat(ref result.Endurance, a.Endurance);
        CrossInt(ref result.Yield, a.Yield);
        CrossFloat(ref result.Lifespan, a.Lifespan);
        CrossFloat(ref result.Maturation, a.Maturation);
        CrossFloat(ref result.Production, a.Production);
        CrossFloat(ref result.Potency, a.Potency);

        CrossBool(ref result.Seedless, a.Seedless); 
        CrossBool(ref result.Ligneous, a.Ligneous); 
        CrossBool(ref result.TurnIntoKudzu, a.TurnIntoKudzu); 
        CrossBool(ref result.CanScream, a.CanScream);

        CrossGasses(ref result.ExudeGasses, a.ExudeGasses);
        CrossGasses(ref result.ConsumeGasses, a.ConsumeGasses);

        result.Mutations = result.Mutations.Where(m => Random(0.5f)).Union(a.Mutations.Where(m => Random(0.5f))).DistinctBy(m => m.Name).ToList();

        // Hybrids have a high chance of being seedless. Balances very
        // effective hybrid crossings.
        if (a.Name != result.Name && Random(0.7f))
        {
            result.Seedless = true;
        }

        return result;
    }

    private void MutateBool(ref bool val, bool polarity, int bits, int totalbits, float mult)
    {
        // Probability that a bit flip happens for this value.
        float probSet = mult * bits / totalbits;
        probSet = Math.Clamp(probSet, 0, 1);
        if (!Random(probSet))
            return;

        val = polarity;
    }

    private void MutateHarvestType(ref HarvestType val, int bits, int totalbits, float mult)
    {
        float probModify = mult * bits / totalbits;
        probModify = Math.Clamp(probModify, 0, 1);

        if (!Random(probModify))
            return;

        if (val == HarvestType.NoRepeat)
            val = HarvestType.Repeat;
        else if (val == HarvestType.Repeat)
            val = HarvestType.SelfHarvest;
    }

    private void MutateGasses(ref Dictionary<Gas, float> gasses, float min, float max, int bits, int totalbits, float mult)
    {
        float probModify = mult * bits / totalbits;
        probModify = Math.Clamp(probModify, 0, 1);
        if (!Random(probModify))
            return;

        // Add a random amount of a random gas to this gas dictionary
        float amount = _robustRandom.NextFloat(min, max);
        Gas gas = _robustRandom.Pick(Enum.GetValues(typeof(Gas)).Cast<Gas>().ToList());
        if (gasses.ContainsKey(gas))
        {
            gasses[gas] += amount;
        }
        else
        {
            gasses.Add(gas, amount);
        }
    }

    private void MutateChemicals(ref Dictionary<string, SeedChemQuantity> chemicals, int bits, int totalbits, float mult)
    {
        float probModify = mult * bits / totalbits;
        probModify = Math.Clamp(probModify, 0, 1);
        if (!Random(probModify))
            return;

        // Add a random amount of a random chemical to this set of chemicals
        if (_randomChems != null)
        {
            var pick = _randomChems.Pick(_robustRandom);
            string chemicalId = pick.reagent;
            int amount = _robustRandom.Next(1, (int)pick.quantity);
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

    private void CrossChemicals(ref Dictionary<string, SeedChemQuantity> val, Dictionary<string, SeedChemQuantity> other)
    {
        // Go through chemicals from the pollen in swab
        foreach (var otherChem in other)
        {
            // if both have same chemical, randomly pick potency ratio from the two.
            if (val.ContainsKey(otherChem.Key))
            {
                val[otherChem.Key] = Random(0.5f) ? otherChem.Value : val[otherChem.Key];
            }
            // if target plant doesn't have this chemical, has 50% chance to add it.
            else
            {
                if (Random(0.5f))
                {
                    var fixedChem = otherChem.Value;
                    fixedChem.Inherent = false;
                    val.Add(otherChem.Key, fixedChem);
                }
            }
        }

        // if the target plant has chemical that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisChem in val)
        {
            if (!other.ContainsKey(thisChem.Key))
            {
                if (Random(0.5f))
                {
                    if (val.Count > 1)
                    {
                        val.Remove(thisChem.Key);
                    }
                }
            }
        }
    }

    private void CrossGasses(ref Dictionary<Gas, float> val, Dictionary<Gas, float> other)
    {
        // Go through gasses from the pollen in swab
        foreach (var otherGas in other)
        {
            // if both have same gas, randomly pick ammount from the two.
            if (val.ContainsKey(otherGas.Key))
            {
                val[otherGas.Key] = Random(0.5f) ? otherGas.Value : val[otherGas.Key];
            }
            // if target plant doesn't have this gas, has 50% chance to add it.
            else
            {
                if (Random(0.5f))
                {
                    val.Add(otherGas.Key, otherGas.Value);
                }
            }
        }
        // if the target plant has gas that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisGas in val)
        {
            if (!other.ContainsKey(thisGas.Key))
            {
                if (Random(0.5f))
                {
                    val.Remove(thisGas.Key);
                }
            }
        }
    }
    private void CrossFloat(ref float val, float other)
    {
        val = Random(0.5f) ? val : other;
    }

    private void CrossInt(ref int val, int other)
    {
        val = Random(0.5f) ? val : other;
    }

    private void CrossBool(ref bool val, bool other)
    {
        val = Random(0.5f) ? val : other;
    }

    private bool Random(float p)
    {
        return _robustRandom.Prob(p);
    }
}
