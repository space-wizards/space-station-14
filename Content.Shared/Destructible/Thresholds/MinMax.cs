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
    /// Returns a random integer within this range, including both bounds.
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <returns>Random value between <see cref="Min"/> (inclusive) and <see cref="Max"/> (inclusive)</returns>
    /// <seealso cref="IRobustRandom.Next(int, int)"/>
    public readonly int Next(IRobustRandom random)
    {
        return random.Next((int)Min, (int)Max + 1);
    }

    /// <summary>
    /// Returns a random float within this range, including <see cref="Min"/> and excluding <see cref="Max"/>.
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <returns>Random value between <see cref="Min"/> (inclusive) and <see cref="Max"/> (exclusive)</returns>
    /// <seealso cref="IRobustRandom.NextFloat(float, float)"/>
    public readonly float NextFloat(IRobustRandom random)
    {
        return random.NextFloat(Min, Max);
    }

    public static implicit operator MinMax((int Min, int Max) tuple)
    {
        return new MinMax(tuple.Min, tuple.Max);
    }
}
