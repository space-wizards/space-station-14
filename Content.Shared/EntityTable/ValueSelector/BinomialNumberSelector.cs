using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Gives a value between the two numbers specified, inclusive.
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
    /// my preferred "Prob" was already used in other places for entity table stuff and I didnt want more confusing terminology
    [DataField]
    public float Chance = .5f;

    public override int Get(System.Random rand)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var count = new int();

        for (int i = 0; i < Trials; i++)
        {
            if (random.Prob(Chance))
                count++;
        }
        return count;
        // get binomialed motherfucker
    }
}
