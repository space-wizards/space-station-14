using Robust.Shared.Random;

namespace Content.Shared.Destructible.Thresholds;

[DataDefinition, Serializable]
public partial struct MinMax
{
    [DataField]
    public float Min;

    [DataField]
    public float Max;

    public MinMax(float min, float max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// wrapper for RobustRandom.Next using the values in this minMax (adding 1 to Max to make it inclusive)
    /// </summary>
    /// <param name="random">RobustRandom instance to use</param>
    /// <returns>Random value between <see cref="Min"/> (inclusive) and <see cref="Max"/> (inclusive)</returns>
    /// <seealso cref="IRobustRandom.Next(int, int)"/>
    public readonly int Next(IRobustRandom random)
    {
        return random.Next((int)Min, (int)Max + 1);
    }

    /// <summary>
    /// wrapper for RobustRandom.NextFloat using the values in this minMax
    /// </summary>
    /// <param name="random">RobustRandom instance to use</param>
    /// <param name="maxOffset">Value to add to 'Max' in internal NextFloat call, can be used to make max value not exclusive</param>
    /// <returns>Random value between <see cref="Min"/> (inclusive) and <see cref="Max"/> (exclusive)</returns>
    /// <seealso cref="IRobustRandom.NextFloat(float, float)"/>
    public readonly float NextFloat(IRobustRandom random, float maxOffset = 0)
    {
        return random.NextFloat(Min, Max + maxOffset);
    }

    public static implicit operator MinMax((int Min, int Max) tuple)
    {
        return new MinMax(tuple.Min, tuple.Max);
    }
}
