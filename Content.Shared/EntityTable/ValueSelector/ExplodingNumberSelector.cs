using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Picks a value by continuously rolling a P chance until it fails or hits its max
/// </summary>
public sealed partial class ExplodingNumberSelector : NumberSelector
{
    /// <summary>
    /// The odds a single trial succeeds
    /// </summary>
    [DataField]
    public float Chance = .5f;

    /// <summary>
    /// The minimum value this can return.
    /// </summary>
    [DataField]
    public int Min;

    /// <summary>
    /// The highest value this can return. When null, this is ignored.
    /// </summary>
    [DataField]
    public int? Max;

    public override int Get(IRobustRandom rand)
    {
        var count = Min;

        while (Max is null || Max > count)
        {
            if (!rand.Prob(Chance))
                break;

            count++;
        }

        return count;
    }

    public override float Odds()
    {
        return Min == 0 ? Chance : 1f;
    }

    public override float Average()
    {
        if (Max is null)
            return Chance / (1 - Chance) + Min; // Sum of an infinite geometric series.

        return Chance * (1 - MathF.Pow(Chance, Max.Value - Min + 1)) / (1 - Chance) + Min; // Sum of a finite Geometric series.
    }
}
