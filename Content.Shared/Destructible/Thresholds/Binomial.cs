using Robust.Shared.Random;

namespace Content.Shared.Destructible.Thresholds;

[DataDefinition, Serializable]
public partial struct Binomial
{
    /// <summary>
    ///     How many times to try including an entry, i.e. the Max.
    /// </summary>
    [DataField]
    public int Trials;

    /// <summary>
    ///     The odds a single trial succeeds.
    /// </summary>
    [DataField]
    public float Chance = .5f;

    public Binomial(int trials, float chance)
    {
        Trials = trials;
        Chance = chance;
    }

    public readonly int Next(IRobustRandom random)
    {
        int count = 0;

        for (int i = 0; i < Trials; i++)
        {
            if (random.Prob(Chance))
                count++;
        }
        return count;
    }

    public readonly int Next(System.Random random)
    {
        int count = 0;

        for (int i = 0; i < Trials; i++)
        {
            if (random.Prob(Chance))
                count++;
        }
        return count;
    }
}
