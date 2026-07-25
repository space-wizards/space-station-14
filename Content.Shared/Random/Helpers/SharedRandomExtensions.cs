using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Random.Helpers;

public static class SharedRandomExtensions
{
    public static string Pick(this IRobustRandom random, DatasetPrototype prototype)
    {
        return random.Pick(prototype.Values);
    }

    /// <summary>
    /// Randomly selects an entry from <paramref name="prototype"/>, attempts to localize it, and returns the result.
    /// </summary>
    public static string Pick(this IRobustRandom random, LocalizedDatasetPrototype prototype)
    {
        var index = random.Next(prototype.Values.Count);
        return Loc.GetString(prototype.Values[index]);
    }

    public static string Pick(this IWeightedRandomPrototype prototype, IRobustRandom? random = null)
    {
        IoCManager.Resolve(ref random);
        var picks = prototype.Weights;
        var sum = picks.Values.Sum();
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var (key, weight) in picks)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return key;
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    public static (string reagent, FixedPoint2 quantity) Pick(this WeightedRandomFillSolutionPrototype prototype, IRobustRandom? random = null)
    {
        var randomFill = prototype.PickRandomFill(random);

        IoCManager.Resolve(ref random);

        var sum = randomFill.Reagents.Count;
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var reagent in randomFill.Reagents)
        {
            accumulated += 1f;

            if (accumulated >= rand)
            {
                return (reagent, randomFill.Quantity);
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    public static RandomFillSolution PickRandomFill(this WeightedRandomFillSolutionPrototype prototype, IRobustRandom? random = null)
    {
        IoCManager.Resolve(ref random);

        var fills = prototype.Fills;
        Dictionary<RandomFillSolution, float> picks = new();

        foreach (var fill in fills)
        {
            picks[fill] = fill.Weight;
        }

        var sum = picks.Values.Sum();
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var (randSolution, weight) in picks)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return randSolution;
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick for {prototype.ID}!");
    }

    // TODO: REPLACE ALL OF THIS WITH PREDICTED RANDOM WHEN ENGINE PR IS MERGED
    /// <summary>
    /// Creates an instance of IRobustRandom that will be the same for both the server and client.
    /// This allows for the client and server to roll the same results when determining things randomly, preventing mispredictions.
    /// We generate a unique seed by getting 2-3 unique but predictable integers into a Hashcode.
    /// </summary>
    /// <param name="timing">An instance if IGameTiming.
    /// We use the integer value of the current tick to ensure a different seed every tick.</param>
    /// <param name="netEnt">The relevant net entity to our seed.
    /// This allows different entities to have different seeds and therefore different results on the same game-tick.</param>
    /// <param name="netEnt2">An optional relevant net entity to our seed.
    /// Typically used if we have an entity checking random potentially multiple times per tick, to ensure we get a unique seed each time.
    /// This entity should not be the same entity as <see cref="netEnt"/>.</param>
    public static IRobustRandom PredictedRandom(IGameTiming timing, NetEntity netEnt, NetEntity? netEnt2 = null)
    {
        var seed = HashCodeCombine((int)timing.CurTick.Value, netEnt.Id, netEnt2?.Id ?? 0);
        var random = new RobustRandom();
        random.SetSeed(seed);
        return random;
    }

    /// <summary>
    /// Checks a probability against a <see cref="PredictedRandom"/> instance.
    /// Returns true if the amount rolled is below the probability.
    /// </summary>
    public static bool PredictedProb(IGameTiming timing, float probability, NetEntity netEnt1, NetEntity? netEnt2 = null)
    {
        var rand = PredictedRandom(timing, netEnt1, netEnt2);
        return rand.Prob(probability);
    }
}
