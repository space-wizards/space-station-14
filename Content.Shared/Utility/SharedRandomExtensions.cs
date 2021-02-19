using Content.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Utility
{
    public static class SharedRandomExtensions
    {
        public static string Pick(this IRobustRandom random, DatasetPrototype prototype)
        {
            return random.Pick(prototype.Values);
        }
    }
}
