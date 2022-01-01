using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Species;

public class SpeciesManager
{
    public const string DefaultSpecies = "Human";

    //HACK: Makes the database actually able to verify a species is valid.
    public static IReadOnlyDictionary<string, SpeciesPrototype> SpeciesIdToProto = default!;

    public void Initialize()
    {
        SpeciesIdToProto = IoCManager.Resolve<IPrototypeManager>()
            .EnumeratePrototypes<SpeciesPrototype>().ToDictionary(x => x.ID, x => x);
    }
}
