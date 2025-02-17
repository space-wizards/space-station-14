using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;

public sealed partial class ReactionJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();

        var reactions =
            prototype
                .EnumeratePrototypes<ReactionPrototype>()
                .Select(x => new ReactionEntry(x))
                .ToDictionary(x => x.Id, x => x);

        // Corvax-Wiki-Start
        if (reactions is not null) AddMixingCategories(reactions, prototype);
        // Corvax-Wiki-End

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals, // Corvax-Wiki
            Converters =
            {
                new UniversalJsonConverter<EntityEffect>(),
            }
        };

        file.Write(JsonSerializer.Serialize(reactions, serializeOptions));
    }
}

