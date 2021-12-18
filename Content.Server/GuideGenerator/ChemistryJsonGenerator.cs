using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;

public class ChemistryJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var prototypes =
            prototype
                .EnumeratePrototypes<ReagentPrototype>()
                .Where(x => !x.Abstract)
                .Select(x => new ReagentEntry(x))
                .ToDictionary(x => x.Id, x => x);

        var reactions =
            prototype
                .EnumeratePrototypes<ReactionPrototype>()
                .Where(x => x.Products.Count != 0);

        foreach (var reaction in reactions)
        {
            foreach (var product in reaction.Products.Keys)
            {
                prototypes[product].Recipes.Add(reaction.ID);
            }
        }

        file.Write(JsonSerializer.Serialize(prototypes));
    }
}
