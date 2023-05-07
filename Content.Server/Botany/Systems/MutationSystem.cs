using Robust.Shared.Random;

namespace Content.Server.Botany;

public sealed class MutationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

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
    public void MutateSeed(SeedData seed, float severity)
    {
        if (!seed.Unique)
        {
            Logger.Error($"Attempted to mutate a shared seed");
            return;
        }

        // Add up everything in the bits column and put the number here.
        const int totalbits = 245;

        // Tolerances (55)
        MutateFloat(ref seed.NutrientConsumption   , 0.05f , 1.2f , 5 , totalbits , severity);
        MutateFloat(ref seed.WaterConsumption      , 3f    , 9f   , 5 , totalbits , severity);
        MutateFloat(ref seed.IdealHeat             , 263f  , 323f , 5 , totalbits , severity);
        MutateFloat(ref seed.HeatTolerance         , 2f    , 25f  , 5 , totalbits , severity);
        MutateFloat(ref seed.IdealLight            , 0f    , 14f  , 5 , totalbits , severity);
        MutateFloat(ref seed.LightTolerance        , 1f    , 5f   , 5 , totalbits , severity);
        MutateFloat(ref seed.ToxinsTolerance       , 1f    , 10f  , 5 , totalbits , severity);
        MutateFloat(ref seed.LowPressureTolerance  , 60f   , 100f , 5 , totalbits , severity);
        MutateFloat(ref seed.HighPressureTolerance , 100f  , 140f , 5 , totalbits , severity);
        MutateFloat(ref seed.PestTolerance         , 0f    , 15f  , 5 , totalbits , severity);
        MutateFloat(ref seed.WeedTolerance         , 0f    , 15f  , 5 , totalbits , severity);

        // Stats (30*2 = 60)
        MutateFloat(ref seed.Endurance             , 50f   , 150f , 5 , totalbits , 2*severity);
        MutateInt(ref seed.Yield                   , 3     , 10   , 5 , totalbits , 2*severity);
        MutateFloat(ref seed.Lifespan              , 10f   , 80f  , 5 , totalbits , 2*severity);
        MutateFloat(ref seed.Maturation            , 3f    , 8f   , 5 , totalbits , 2*severity);
        MutateFloat(ref seed.Production            , 1f    , 10f  , 5 , totalbits , 2*severity);
        MutateFloat(ref seed.Potency               , 30f   , 100f , 5 , totalbits , 2*severity);

        // Kill the plant (30)
        MutateBool(ref seed.Viable         , false , 30 , totalbits , severity);

        // Fun (90)
        MutateBool(ref seed.Seedless       , true  , 10 , totalbits , severity);
        MutateBool(ref seed.Slip           , true  , 10 , totalbits , severity);
        MutateBool(ref seed.Sentient       , true  , 10 , totalbits , severity);
        MutateBool(ref seed.Ligneous       , true  , 10 , totalbits , severity);
        MutateBool(ref seed.Bioluminescent , true  , 10 , totalbits , severity);
        MutateBool(ref seed.TurnIntoKudzu  , true  , 10 , totalbits , severity);
        MutateBool(ref seed.CanScream      , true  , 10 , totalbits , severity);
        seed.BioluminescentColor = RandomColor(seed.BioluminescentColor, 10, totalbits, severity);
        // ConstantUpgade (10)
        MutateHarvestType(ref seed.HarvestRepeat   , 10 , totalbits , severity);
    }

    public SeedData Cross(SeedData a, SeedData b)
    {
        SeedData result = b.Clone();

        result.Chemicals = Random(0.5f) ? a.Chemicals : result.Chemicals;

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
        CrossBool(ref result.TurnIntoKudzu, a.TurnIntoKudzu);
        CrossBool(ref result.CanScream, a.CanScream);
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
        // Probability that a bit flip happens for this value.
        float p = mult*bits/totalbits;
        if (!Random(p))
        {
            return;
        }

        // Starting number of bits that are high, between 0 and bits.
        int n = (int)Math.Round((val - min) / (max - min) * bits);
        // val may be outside the range of min/max due to starting prototype values, so clamp
        n = Math.Clamp(n, 0, bits);

        // Probability that the bit flip increases n.
        float p_increase = 1-(float)n/bits;
        int np;
        if (Random(p_increase))
        {
            np = n + 1;
        }
        else
        {
            np = n - 1;
        }

        // Set value based on mutated thermometer code.
        float nval = MathF.Min(MathF.Max((float)np/bits * (max - min) + min, min), max);
        val = nval;
    }

    private void MutateInt(ref int n, int min, int max, int bits, int totalbits, float mult)
    {
        // Probability that a bit flip happens for this value.
        float p = mult*bits/totalbits;
        if (!Random(p))
        {
            return;
        }

        // Probability that the bit flip increases n.
        float p_increase = 1-(float)n/bits;
        int np;
        if (Random(p_increase))
        {
            np = n + 1;
        }
        else
        {
            np = n - 1;
        }

        np = Math.Min(Math.Max(np, min), max);
        n = np;
    }

    private void MutateBool(ref bool val, bool polarity, int bits, int totalbits, float mult)
    {
        // Probability that a bit flip happens for this value.
        float p = mult*bits/totalbits;
        if (!Random(p))
        {
            return;
        }

        val = polarity;
    }

    private void MutateHarvestType(ref HarvestType val, int bits, int totalbits, float mult)
    {
        float p = mult * bits/totalbits;
        if (!Random(p))
            return;

        if (val == HarvestType.NoRepeat)
            val = HarvestType.Repeat;

        else if (val == HarvestType.Repeat)
            val = HarvestType.SelfHarvest;
    }

    private Color RandomColor(Color color, int bits, int totalbits, float mult)
    {
        float p = mult*bits/totalbits;
        if (Random(p))
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
            var rng = IoCManager.Resolve<IRobustRandom>();
            return rng.Pick(colors);
        }
        return color;
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
