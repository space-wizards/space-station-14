using System.IO;
using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests;

public sealed class ActionMigrator
{
    [Test]
    public async Task Main()
    {
        await using var pair = await PoolManager.GetServerClient();
        var path = "C:\\Projects\\space-station-14\\Resources\\mapping_actions.yml";

        TextReader reader = File.OpenText(path);

        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        if (yamlStream.Documents[0].RootNode.ToDataNode() is not SequenceDataNode sequence)
            return;

        var serialization = pair.Server.ResolveDependency<ISerializationManager>();
        var actions = new SequenceDataNode();

        foreach (var entry in sequence.Sequence)
        {
            if (entry is not MappingDataNode mappingAction)
                continue;

            if (!mappingAction.TryGet<MappingDataNode>("action", out var map))
                return;

            var tag = map.Tag?.Replace("!type:", "");

            if (map.TryGet("name", out var name))
            {
                // mappingAction["id"] = new ValueDataNode("ActionMapping" + name);
                mappingAction["name"] = name;
            }

            if (map.TryGet("description", out var description))
            {
                mappingAction["description"] = description;
            }

            map.Remove("name");
            map.Remove("description");

            map.Tag = null;

            BaseActionComponent comp = tag switch
            {
                "InstantAction" => serialization.Read<InstantActionComponent>(map),
                "EntityTargetAction" => serialization.Read<EntityTargetActionComponent>(map),
                "WorldTargetAction" => serialization.Read<WorldTargetActionComponent>(map),
                _ => throw new ArgumentOutOfRangeException()
            };

            mappingAction.Remove("action");
            mappingAction["action"] = serialization.WriteValue(comp);
            actions.Add(mappingAction);
        }

        reader.Dispose();

        var writeStream = new YamlStream();
        writeStream.Add(new YamlDocument(actions.ToYamlNode()));

        var fileWrite = File.OpenWrite(path);
        var writer = new StreamWriter(fileWrite);
        writeStream.Save(new YamlMappingFix(new Emitter(writer)), false);
        writer.Close();

        Console.WriteLine();
    }
}
