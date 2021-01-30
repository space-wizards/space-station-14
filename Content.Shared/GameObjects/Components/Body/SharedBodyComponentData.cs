#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Preset;
using Content.Shared.GameObjects.Components.Body.Template;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Body
{
    public partial class SharedBodyComponentData
    {
        [CustomYamlField("templateName")] public string? TemplateName;

        [CustomYamlField("connections")]
        public Dictionary<string, List<string>>? Connections;

        [CustomYamlField("slots")]
        public Dictionary<string, BodyPartType>? Slots;

        [CustomYamlField("centerSlot")]
        public string? _centerSlot;

        [CustomYamlField("partIds")]
        public Dictionary<string, string>? _partIds;

        [CustomYamlField("presetName")]
        public string? PresetName { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            serializer.DataReadWriteFunction(
                "template",
                null,
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return;
                    }

                    var template = prototypeManager.Index<BodyTemplatePrototype>(name);

                    Connections = template.Connections;
                    Slots = template.Slots;
                    _centerSlot = template.CenterSlot;

                    TemplateName = name;
                },
                () => TemplateName);

            serializer.DataReadWriteFunction(
                "preset",
                null,
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return;
                    }

                    var preset = prototypeManager.Index<BodyPresetPrototype>(name);

                    _partIds = preset.PartIDs;
                    PresetName = preset.Name;
                },
                () => PresetName);

            Connections ??= new();
            serializer.DataReadWriteFunction(
                "connections",
                new Dictionary<string, List<string>>(),
                connections =>
                {
                    foreach (var (from, to) in connections)
                    {
                        Connections.GetOrNew(from).AddRange(to);
                    }
                },
                () => Connections);
            if (Connections.Count == 0) Connections = null;

            Slots ??= new();
            serializer.DataReadWriteFunction(
                "slots",
                new Dictionary<string, BodyPartType>(),
                slots =>
                {
                    foreach (var (part, type) in slots)
                    {
                        Slots[part] = type;
                    }
                },
                () => Slots);
            if (Slots.Count == 0) Slots = null;

            // TODO BODY Move to template or somewhere else
            serializer.DataReadWriteFunction(
                "centerSlot",
                null,
                slot => _centerSlot = slot,
                () => _centerSlot);

            _partIds ??= new();
            serializer.DataReadWriteFunction(
                "partIds",
                new Dictionary<string, string>(),
                partIds =>
                {
                    foreach (var (slot, part) in partIds)
                    {
                        _partIds[slot] = part;
                    }
                },
                () => _partIds);
            if (_partIds.Count == 0) _partIds = null;

            // Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
            // The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
            var cleanedConnections = new Dictionary<string, List<string>>();
            Slots ??= new();
            Connections ??= new();
            foreach (var targetSlotName in Slots.Keys)
            {
                var tempConnections = new List<string>();
                foreach (var (slotName, slotConnections) in Connections)
                {
                    if (slotName == targetSlotName)
                    {
                        foreach (var connection in slotConnections)
                        {
                            if (!tempConnections.Contains(connection))
                            {
                                tempConnections.Add(connection);
                            }
                        }
                    }
                    else if (slotConnections.Contains(targetSlotName))
                    {
                        tempConnections.Add(slotName);
                    }
                }

                if (tempConnections.Count > 0)
                {
                    cleanedConnections.Add(targetSlotName, tempConnections);
                }
            }

            Connections = cleanedConnections;

            if (Connections.Count == 0) Connections = null;
            if (Slots.Count == 0) Slots = null;
        }
    }
}
