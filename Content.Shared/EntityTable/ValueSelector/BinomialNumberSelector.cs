using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Picks a value based on a Binomial Distribution of N Trials given P Chance
/// </summary>
public sealed partial class BinomialNumberSelector : NumberSelector
{
    /// <summary>
    /// How many times to try including an entry. i.e. the Max.
    /// </summary>
    [DataField]
    public int Trials = 1;

    /// <summary>
    /// The odds a single trial succeeds
    /// </summary>
    /// <remarks>
    /// my preferred "Prob" was already used in other places for entity table stuff and I didnt want more confusing terminology
    /// </remarks>
    [DataField]
    public float Chance = .5f;

    public override int Get(System.Random rand)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        int count = 0;

        for (int i = 0; i < Trials; i++)
        {
            if (random.Prob(Chance))
                count++;
        }
        return count;
        // get binomialed motherfucker
    }
}
