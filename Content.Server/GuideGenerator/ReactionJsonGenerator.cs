using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;

public sealed class ReactionJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();

        var reactions =
            prototype
                .EnumeratePrototypes<ReactionPrototype>()
                .Select(x => new ReactionEntry(x))
                .ToDictionary(x => x.Id, x => x);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new UniversalJsonConverter<ReagentEffect>(),
            }
        };

        file.Write(JsonSerializer.Serialize(reactions, serializeOptions));
    }
}

