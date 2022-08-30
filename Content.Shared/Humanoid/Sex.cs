using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid
{
    public enum Sex : byte
    {
        Male,
        Female
    }

    public static class SexExtensions
    {
        public static string GetName(this Sex sex, string species, IPrototypeManager? prototypeManager = null, IRobustRandom? random = null)
        {
            IoCManager.Resolve(ref prototypeManager);
            IoCManager.Resolve(ref random);

            // if they have an old species or whatever just fall back to human I guess?
            // Some downstream is probably gonna have this eventually but then they can deal with fallbacks.
            if (!prototypeManager.TryIndex(species, out SpeciesPrototype? speciesProto))
            {
                speciesProto = prototypeManager.Index<SpeciesPrototype>("Human");
                Logger.Warning($"Unable to find species {species} for name, falling back to Human");
            }

            switch (speciesProto.Naming)
            {
                case SpeciesNaming.FirstDashFirst:
                    return $"{GetFirstName(sex, speciesProto, prototypeManager, random)}-{GetFirstName(sex, speciesProto, prototypeManager, random)}";
                case SpeciesNaming.FirstLast:
                default:
                    return $"{GetFirstName(sex, speciesProto, prototypeManager, random)} {GetLastName(sex, speciesProto, prototypeManager, random)}";
            }
        }

        public static string GetFirstName(this Sex sex, SpeciesPrototype speciesProto, IPrototypeManager? protoManager = null, IRobustRandom? random = null)
        {
            IoCManager.Resolve(ref protoManager);
            IoCManager.Resolve(ref random);

            switch (sex)
            {
                case Sex.Male:
                    return random.Pick(protoManager.Index<DatasetPrototype>(speciesProto.MaleFirstNames).Values);
                case Sex.Female:
                    return random.Pick(protoManager.Index<DatasetPrototype>(speciesProto.FemaleFirstNames).Values);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string GetLastName(this Sex sex, SpeciesPrototype speciesProto, IPrototypeManager? protoManager = null, IRobustRandom? random = null)
        {
            IoCManager.Resolve(ref protoManager);
            IoCManager.Resolve(ref random);
            return random.Pick(protoManager.Index<DatasetPrototype>(speciesProto.LastNames).Values);
        }
    }
}
