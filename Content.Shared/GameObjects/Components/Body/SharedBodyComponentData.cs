#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Preset;
using Content.Shared.GameObjects.Components.Body.Template;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body
{
    public partial class SharedBodyComponentData : ISerializationHooks
    {
        [DataField("template", required: true)] [DataClassTarget("templateName")]
        public string TemplateName = default!;

        [DataClassTarget("connections")]
        public Dictionary<string, List<string>>? Connections;

        [DataClassTarget("slots")]
        public Dictionary<string, BodyPartType>? Slots;

        [DataClassTarget("centerSlot")]
        public string? _centerSlot;

        [DataClassTarget("partIds")]
        public Dictionary<string, string>? _partIds;

        [DataField("preset", required: true)]
        [DataClassTarget("preset")]
        public string PresetName { get; private set; } = default!;

        public void BeforeSerialization()
        {
            throw new System.NotImplementedException();
        }

        public void AfterDeserialization()
        {
            // TODO BODY Move to template or somewhere else
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            var template = prototypeManager.Index<BodyTemplatePrototype>(TemplateName);

            Connections = template.Connections;
            Slots = template.Slots;
            _centerSlot = template.CenterSlot;

            var preset = prototypeManager.Index<BodyPresetPrototype>(PresetName);

            _partIds = preset.PartIDs;

            // Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
            // The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
            var cleanedConnections = new Dictionary<string, List<string>>();
            Slots ??= new Dictionary<string, BodyPartType>();
            Connections ??= new Dictionary<string, List<string>>();

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
