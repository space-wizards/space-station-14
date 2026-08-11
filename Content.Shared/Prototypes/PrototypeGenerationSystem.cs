using System.IO;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Labels.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Upload;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes;

/// <summary>
///     System that handles generating YML at runtime.
///     It raises an event that other systems can handle.
///     <see cref="PrototypeGenerationEvent"/>
///     The server registers these changes and sends them to clients,
///     also saving them to replays.
/// </summary>
public sealed partial class PrototypeGenerationSystem : EntitySystem
{
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGamePrototypeLoadManager _gamePrototypeLoad = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ISerializationManager _serialization = default!;

    public void Generate()
    {
        if (_net.IsClient)
            return;

        var ev = new PrototypeGenerationEvent(new List<(string Id, EntityBuilder Builder)>());
        RaiseLocalEvent(ref ev);

        var root = new YamlSequenceNode();
        foreach (var (id, builder) in ev.Ents)
        {
            var entity = new MappingDataNode
            {
                ["type"] = new ValueDataNode("entity"),
                [IdDataFieldAttribute.Name] = new ValueDataNode(id),
            };

            if (builder.Parents != null)
                entity[ParentDataFieldAttribute.Name] = new SequenceDataNode(builder.Parents);

            if (builder.Name != null)
                entity["name"] = new ValueDataNode(builder.Name);

            if (builder.Description != null)
                entity["description"] = new ValueDataNode(builder.Description);

            if (builder.Suffix != null)
                entity["suffix"] = new ValueDataNode(builder.Suffix);

            if (builder.Components is { Count: > 0 })
            {
                var registry = new ComponentRegistry();
                foreach (var comp in builder.Components)
                {
                    registry.Add(_compFactory.CompName(comp.GetType()), new EntityPrototype.ComponentRegistryEntry(comp));
                }

                entity["components"] = _serialization.WriteValue(registry, notNullableOverride: true);
            }

            root.Add(entity.ToYamlNode());
        }

        var stream = new YamlStream();
        stream.Add(new YamlDocument(root));

        var writer = new StringWriter();
        stream.Save(new YamlNoDocEndDotsFix(new YamlMappingFix(new Emitter(writer))), false);

        _gamePrototypeLoad.SendGamePrototype(writer.ToString());
    }

    [SubscribeLocalEvent]
    public void OnGeneration(ref PrototypeGenerationEvent ev)
    {
#pragma warning disable RA0002
        foreach (var reagent in _prototype.EnumeratePrototypes<ReagentPrototype>())
        {
            var ent = new EntityBuilder
                {
                    Parents = ["BaseChemistryBottleFilled"],
                    Name = $"{reagent.LocalizedName} bottle",
                }
                .AddComp(new LabelComponent { CurrentLabel = reagent.LocalizedName })
                .AddComp(new SolutionComponent
                {
                    Solution = new Solution
                    {
                        Contents =
                        {
                            new ReagentQuantity(reagent.ID, 30),
                        },
                    },
                });

            ev.AddEntity($"GeneratedBottle{reagent.ID}", ent);
        }
#pragma warning restore RA0002
    }
}
