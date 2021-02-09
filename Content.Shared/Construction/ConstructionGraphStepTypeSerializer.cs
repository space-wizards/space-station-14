using System;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    public class ConstructionGraphStepTypeSerializer : YamlObjectSerializer.TypeSerializer
    {
        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
        {
            if (serializer.TryReadDataField("material", out MaterialConstructionGraphStep material))
            {
                return material;
            }

            if (serializer.TryReadDataField("tool", out ToolConstructionGraphStep tool))
            {
                return tool;
            }

            if (serializer.TryReadDataField("prototype", out PrototypeConstructionGraphStep prototype))
            {
                return prototype;
            }

            if (serializer.TryReadDataField("component", out ComponentConstructionGraphStep component))
            {
                return component;
            }

            if(serializer.TryReadDataField("steps", out NestedConstructionGraphStep nested))
            {
                return nested;
            }

            throw new ArgumentException("Tried to convert invalid YAML node mapping to ConstructionGraphStep!");
        }

        public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
        {
            return new YamlMappingNode();
        }
    }
}
