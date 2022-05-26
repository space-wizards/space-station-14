using Content.Shared.Dataset;
using Robust.Shared.Random;

namespace Content.Shared.Random.Helpers
{
    public static class SharedRandomExtensions
    {
        public static string Pick(this IRobustRandom random, DatasetPrototype prototype)
        {
            return random.Pick(prototype.Values);
        }
    }
}
