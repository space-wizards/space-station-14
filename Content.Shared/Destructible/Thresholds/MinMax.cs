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

    public readonly int Next(IRobustRandom random)
    {
        return random.Next((int)Min, (int)Max + 1);
    }

    public readonly float NextFloat(IRobustRandom random)
    {
        return random.NextFloat(Min, Max + 1);
    }

    public static implicit operator MinMax((int Min, int Max) tuple)
    {
        return new MinMax(tuple.Min, tuple.Max);
    }
}
