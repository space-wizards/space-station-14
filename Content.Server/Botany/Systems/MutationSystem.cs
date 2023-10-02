using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Reagent;
using System.Linq;
using Content.Shared.Atmos;
using FastAccessors;

namespace Content.Server.Botany;

public sealed class MutationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private List<ReagentPrototype> _allChemicals = default!;

    public override void Initialize()
    {
        _allChemicals = _prototypeManager.EnumeratePrototypes<ReagentPrototype>().ToList();
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
    public void MutateSeed(ref SeedData seed, float severity)
    {
        if (!seed.Unique)
        {
            Log.Error($"Attempted to mutate a shared seed");
            return;
        }

        // Add up everything in the bits column and put the number here.
        const int totalbits = 265;

        // Tolerances (55)
        MutateFloat(ref seed.NutrientConsumption  , 0.05f, 1.2f, 5, totalbits, severity);
        MutateFloat(ref seed.WaterConsumption     , 3f   , 9f  , 5, totalbits, severity);
        MutateFloat(ref seed.IdealHeat            , 263f , 323f, 5, totalbits, severity);
        MutateFloat(ref seed.HeatTolerance        , 2f   , 25f , 5, totalbits, severity);
        MutateFloat(ref seed.IdealLight           , 0f   , 14f , 5, totalbits, severity);
        MutateFloat(ref seed.LightTolerance       , 1f   , 5f  , 5, totalbits, severity);
        MutateFloat(ref seed.ToxinsTolerance      , 1f   , 10f , 5, totalbits, severity);
        MutateFloat(ref seed.LowPressureTolerance , 60f  , 100f, 5, totalbits, severity);
        MutateFloat(ref seed.HighPressureTolerance, 100f , 140f, 5, totalbits, severity);
        MutateFloat(ref seed.PestTolerance        , 0f   , 15f , 5, totalbits, severity);
        MutateFloat(ref seed.WeedTolerance        , 0f   , 15f , 5, totalbits, severity);

        // Stats (30*2 = 60)
        MutateFloat(ref seed.Endurance            , 50f  , 150f, 5, totalbits, 2 * severity);
        MutateInt(ref seed.Yield                  , 3    , 10  , 5, totalbits, 2 * severity);
        MutateFloat(ref seed.Lifespan             , 10f  , 80f , 5, totalbits, 2 * severity);
        MutateFloat(ref seed.Maturation           , 3f   , 8f  , 5, totalbits, 2 * severity);
        MutateFloat(ref seed.Production           , 1f   , 10f , 5, totalbits, 2 * severity);
        MutateFloat(ref seed.Potency              , 30f  , 100f, 5, totalbits, 2 * severity);

        // Kill the plant (30)
        MutateBool(ref seed.Viable        , false, 30, totalbits, severity);

        // Fun (90)
        MutateBool(ref seed.Seedless      , true , 10, totalbits, severity);
        MutateBool(ref seed.Slip          , true , 10, totalbits, severity);
        MutateBool(ref seed.Sentient      , true , 10, totalbits, severity);
        MutateBool(ref seed.Ligneous      , true , 10, totalbits, severity);
        MutateBool(ref seed.Bioluminescent, true , 10, totalbits, severity);
        // Kudzu disabled until superkudzu bug is fixed
        // MutateBool(ref seed.TurnIntoKudzu , true , 10, totalbits, severity);
        MutateBool(ref seed.CanScream     , true , 10, totalbits, severity);
        seed.BioluminescentColor = RandomColor(seed.BioluminescentColor, 10, totalbits, severity);

        // ConstantUpgade (10)
        MutateHarvestType(ref seed.HarvestRepeat, 10, totalbits, severity);

        // Gas (5)
        MutateGasses(ref seed.ExudeGasses, 0.01f, 0.5f, 4, totalbits, severity);
        MutateGasses(ref seed.ConsumeGasses, 0.01f, 0.5f, 1, totalbits, severity);

        // Chems (20)
        MutateChemicals(ref seed.Chemicals, 5, 20, totalbits, severity);

        // Species (5)
        MutateSpecies(ref seed, 5, totalbits, severity);
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
        CrossBool(ref result.Viable, a.Viable);
        CrossBool(ref result.Slip, a.Slip);
        CrossBool(ref result.Sentient, a.Sentient);
        CrossBool(ref result.Ligneous, a.Ligneous);
        CrossBool(ref result.Bioluminescent, a.Bioluminescent);
        // CrossBool(ref result.TurnIntoKudzu, a.TurnIntoKudzu);
        CrossBool(ref result.CanScream, a.CanScream);

        CrossGasses(ref result.ExudeGasses, a.ExudeGasses);
        CrossGasses(ref result.ConsumeGasses, a.ConsumeGasses);

        result.BioluminescentColor = Random(0.5f) ? a.BioluminescentColor : result.BioluminescentColor;

        // Hybrids have a high chance of being seedless. Balances very
        // effective hybrid crossings.
        if (a.Name == result.Name && Random(0.7f))
        {
            result.Seedless = true;
        }

        return result;
    }

    // Mutate reference 'val' between 'min' and 'max' by pretending the value
    // is representable by a thermometer code with 'bits' number of bits and
    // randomly flipping some of them.
    //
    // 'totalbits' and 'mult' are used only to calculate the probability that
    // one bit gets flipped.
    private void MutateFloat(ref float val, float min, float max, int bits, int totalbits, float mult)
    {
        // Probability that a bit flip happens for this value's representation in thermometer code.
        float probBitflip = mult * bits / totalbits;
        probBitflip = Math.Clamp(probBitflip, 0, 1);
        if (!Random(probBitflip))
            return;

        // Starting number of bits that are high, between 0 and bits.
        // In other words, it's val mapped linearly from range [min, max] to range [0, bits], and then rounded.
        int valInt = (int)MathF.Round((val - min) / (max - min) * bits);
        // val may be outside the range of min/max due to starting prototype values, so clamp.
        valInt = Math.Clamp(valInt, 0, bits);

        // Probability that the bit flip increases n.
        // The higher the current value is, the lower the probability of increasing value is, and the higher the probability of decreasive it it.
        // In other words, it tends to go to the middle.
        float probIncrease = 1 - (float)valInt / bits;
        int valIntMutated;
        if (Random(probIncrease))
        {
            valIntMutated = valInt + 1;
        }
        else
        {
            valIntMutated = valInt - 1;
        }

        // Set value based on mutated thermometer code.
        float valMutated = Math.Clamp((float)valIntMutated / bits * (max - min) + min, min, max);
        val = valMutated;
    }

    private void MutateInt(ref int val, int min, int max, int bits, int totalbits, float mult)
    {
        // Probability that a bit flip happens for this value's representation in thermometer code.
        float probBitflip = mult * bits / totalbits;
        probBitflip = Math.Clamp(probBitflip, 0, 1);
        if (!Random(probBitflip))
            return;

        // Probability that the bit flip increases n.
        // The higher the current value is, the lower the probability of increasing value is, and the higher the probability of decreasive it it.
        // In other words, it tends to go to the middle.
        float probIncrease = 1 - (float)val / bits;
        int valMutated;
        if (Random(probIncrease))
        {
            valMutated = val + 1;
        }
        else
        {
            valMutated = val - 1;
        }

        valMutated = Math.Clamp(valMutated, min, max);
        val = valMutated;
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

    private void MutateChemicals(ref Dictionary<string, SeedChemQuantity> chemicals, int max, int bits, int totalbits, float mult)
    {
        float probModify = mult * bits / totalbits;
        probModify = Math.Clamp(probModify, 0, 1);
        if (!Random(probModify))
            return;

        // Add a random amount of a random chemical to this set of chemicals
        ReagentPrototype selectedChemical = _robustRandom.Pick(_allChemicals);
        if (selectedChemical != null)
        {
            string chemicalId = selectedChemical.ID;
            int amount = _robustRandom.Next(1, max);
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
            int potencyDivisor = (int) Math.Ceiling(100.0f / seedChemQuantity.Max);
            seedChemQuantity.PotencyDivisor = potencyDivisor;
            chemicals[chemicalId] = seedChemQuantity;
        }
    }

    private void MutateSpecies(ref SeedData seed, int bits, int totalbits, float mult)
    {
        float p = mult * bits / totalbits;
        p = Math.Clamp(p, 0, 1);
        if (!Random(p))
            return;

        if (seed.MutationPrototypes.Count == 0)
            return;

        var targetProto = _robustRandom.Pick(seed.MutationPrototypes);
        _prototypeManager.TryIndex(targetProto, out SeedPrototype? protoSeed);

        if (protoSeed == null)
        {
            Log.Error($"Seed prototype could not be found: {targetProto}!");
            return;
        }

        seed = seed.SpeciesChange(protoSeed);
    }

    private Color RandomColor(Color color, int bits, int totalbits, float mult)
    {
        float probModify = mult * bits / totalbits;
        if (Random(probModify))
        {
            var colors = new List<Color>{
                Color.White,
                Color.Red,
                Color.Yellow,
                Color.Green,
                Color.Blue,
                Color.Purple,
                Color.Pink
            };
            return _robustRandom.Pick(colors);
        }
        return color;
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
