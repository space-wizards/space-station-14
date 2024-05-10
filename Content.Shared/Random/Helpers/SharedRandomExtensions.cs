using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Shared.Random.Helpers
{
    public static class SharedRandomExtensions
    {
        public static string Pick(this IRobustRandom random, DatasetPrototype prototype)
        {
            return random.Pick(prototype.Values);
        }

        public static string Pick(this IWeightedRandomPrototype prototype, System.Random random)
        {
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

        public static T Pick<T>(this IRobustRandom random, Dictionary<T, float> weights)
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
    }
}
