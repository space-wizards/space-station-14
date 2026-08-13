using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Random.Helpers;

public static class SharedRandomExtensions
{
    extension(IRobustRandom random)
    {
        public string Pick(DatasetPrototype prototype)
        {
            return random.Pick(prototype.Values);
        }

        /// <summary>
        /// Randomly selects an entry from <paramref name="prototype"/>, attempts to localize it, and returns the result.
        /// </summary>
        public string Pick(LocalizedDatasetPrototype prototype)
        {
            var index = random.Next(prototype.Values.Count);
            return Loc.GetString(prototype.Values[index]);
        }

#region float

        public T Pick<T>(Dictionary<T, float> weights)
            where T: notnull
        {
            var sum = weights.Values.Sum();
            var accumulated = 0f;

            var rand = random.NextFloat() * sum;

            foreach (var (key, weight) in weights)
            {
                accumulated += weight;

                if (accumulated >= rand)
                {
                    return key;
                }
            }

            throw new InvalidOperationException("Invalid weighted pick");
        }

        public T PickAndTake<T>(Dictionary<T, float> weights)
            where T : notnull
        {
            var pick = random.Pick(weights);
            weights.Remove(pick);
            return pick;
        }

        public bool TryPickAndTake<T>(Dictionary<T, float> weights, [NotNullWhen(true)] out T? pick)
            where T : notnull
        {
            if (weights.Count == 0)
            {
                pick = default;
                return false;
            }
            pick = random.PickAndTake(weights);
            return true;
        }

#endregion

#region FixedPoint2

        /// <summary>
        /// Get random <see cref="FixedPoint2"/> value between 0 (included) and 1 (excluded).
        /// </summary>
        [PublicAPI]
        public FixedPoint2 NextFixedPoint2()
        {
            return FixedPoint2.FromCents(random.Next(100));
        }

        /// <summary>
        /// Get random <see cref="FixedPoint2"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded).
        /// </summary>
        /// <param name="maxValue">Random value should be less then this value.</param>
        [PublicAPI]
        public FixedPoint2 NextFixedPoint2(FixedPoint2 maxValue)
        {
            return FixedPoint2.FromCents(random.Next(maxValue.Value));
        }

        /// <summary>
        /// Get random <see cref="FixedPoint2"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
        /// </summary>
        /// <param name="minValue">Random value should be greater or equal to this value.</param>
        /// <param name="maxValue">Random value should be less then this value.</param>
        [PublicAPI]
        public FixedPoint2 NextFixedPoint2(FixedPoint2 minValue, FixedPoint2 maxValue)
        {
            return FixedPoint2.FromCents(random.Next(minValue.Value, maxValue.Value));
        }

        [PublicAPI]
        public bool Prob(FixedPoint2 chance)
        {
            DebugTools.Assert(chance <= 0 && chance >= 1, $"Chance must be in the range 0-1. It was {chance}.");

            return chance > random.NextFixedPoint2();
        }

        [PublicAPI]
        public T Pick<T>(Dictionary<T, FixedPoint2> weights)
            where T : notnull
        {
            var sum = weights.Values.Sum();
            var accumulated = FixedPoint2.Zero;

            var rand = random.NextFixedPoint2(sum);

            foreach (var (key, weight) in weights)
            {
                accumulated += weight;

                if (accumulated >= rand)
                {
                    return key;
                }
            }

            throw new InvalidOperationException("Invalid weighted pick");
        }

        [PublicAPI]
        public T PickAndTake<T>(Dictionary<T, FixedPoint2> weights)
            where T : notnull
        {
            var pick = random.Pick(weights);
            weights.Remove(pick);
            return pick;
        }

        [PublicAPI]
        public bool TryPickAndTake<T>(Dictionary<T, FixedPoint2> weights, [NotNullWhen(true)] out T? pick)
            where T : notnull
        {
            if (weights.Count == 0)
            {
                pick = default;
                return false;
            }

            pick = random.PickAndTake(weights);
            return true;
        }

#endregion

#region FixedPoint4

        /// <summary>
        /// Get random <see cref="FixedPoint4"/> value between 0 (included) and 1 (excluded).
        /// </summary>
        [PublicAPI]
        public FixedPoint4 NextFixedPoint4()
        {
            return FixedPoint4.FromTenThousandths(random.NextLong(10000));
        }

        /// <summary>
        /// Get random <see cref="FixedPoint2"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded).
        /// </summary>
        /// <param name="maxValue">Random value should be less then this value.</param>
        [PublicAPI]
        public FixedPoint4 NextFixedPoint4(FixedPoint4 maxValue)
        {
            return FixedPoint4.FromTenThousandths(random.NextLong(maxValue.Value));
        }

        /// <summary>
        /// Get random <see cref="FixedPoint2"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
        /// </summary>
        /// <param name="minValue">Random value should be greater or equal to this value.</param>
        /// <param name="maxValue">Random value should be less then this value.</param>
        [PublicAPI]
        public FixedPoint4 NextFixedPoint4(FixedPoint4 minValue, FixedPoint4 maxValue)
        {
            return FixedPoint4.FromTenThousandths(random.NextLong(minValue.Value, maxValue.Value));
        }

        [PublicAPI]
        public bool Prob(FixedPoint4 chance)
        {
            DebugTools.Assert(chance <= 0 && chance >= 1, $"Chance must be in the range 0-1. It was {chance}.");

            return chance > random.NextFixedPoint4();
        }

        [PublicAPI]
        public T Pick<T>(Dictionary<T, FixedPoint4> weights)
            where T : notnull
        {
            var sum = weights.Values.Sum();
            var accumulated = FixedPoint4.Zero;

            var rand = random.NextFixedPoint4(sum);

            foreach (var (key, weight) in weights)
            {
                accumulated += weight;

                if (accumulated >= rand)
                {
                    return key;
                }
            }

            throw new InvalidOperationException("Invalid weighted pick");
        }

        [PublicAPI]
        public T PickAndTake<T>(Dictionary<T, FixedPoint4> weights)
            where T : notnull
        {
            var pick = random.Pick(weights);
            weights.Remove(pick);
            return pick;
        }

        [PublicAPI]
        public bool TryPickAndTake<T>(Dictionary<T, FixedPoint4> weights, [NotNullWhen(true)] out T? pick)
            where T : notnull
        {
            if (weights.Count == 0)
            {
                pick = default;
                return false;
            }

            pick = random.PickAndTake(weights);
            return true;
        }

#endregion
    }

#region WeightedRandom

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

    public static ProtoId<T> Pick<T>(this IWeightedRandomPrototype<T> prototype, IRobustRandom? random = null)
        where
        T: class, IPrototype
    {
        IoCManager.Resolve(ref random);
        return random.Pick(prototype.Weights);
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

#endregion

#region Misc

    [Obsolete("Use extension method instead.")]
    public static T Pick<T>(Dictionary<T, float> weights, IRobustRandom random)
        where T : notnull
    {
        return random.Pick(weights);
    }

    /// <inheritdoc cref="HashCodeCombine(IReadOnlyCollection{int})"/>
    public static int HashCodeCombine(params int[] values)
    {
        return HashCodeCombine((IReadOnlyCollection<int>)values);
    }

    /// <summary>
    /// A very simple, deterministic djb2 hash function for generating a combined seed for the random number generator.
    /// We can't use HashCode.Combine because that is initialized with a random value, creating different results on the server and client.
    /// </summary>
    /// <example>
    /// Combine the current game tick with a NetEntity Id in order to not get the same random result if this is called multiple times in the same tick.
    /// <code>
    /// var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
    /// </code>
    /// </example>
    public static int HashCodeCombine(IReadOnlyCollection<int> values)
    {
        var hash = 5381;
        foreach (var value in values)
        {
            hash = (hash << 5) + hash + value;
        }
        return hash;
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

#endregion
}
