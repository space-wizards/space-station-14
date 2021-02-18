#nullable enable
using System;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Content.Shared.Construction
{
    public class ConstructionGraphStepTypeSerializer : ITypeSerializer<ConstructionGraphStep>
    {
        // TODO PAUL SERV3
        public ConstructionGraphStep NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            // if (serializer.TryReadDataField("material", out MaterialConstructionGraphStep material))
            // {
            //     return material;
            // }
            //
            // if (serializer.TryReadDataField("tool", out ToolConstructionGraphStep tool))
            // {
            //     return tool;
            // }
            //
            // if (serializer.TryReadDataField("prototype", out PrototypeConstructionGraphStep prototype))
            // {
            //     return prototype;
            // }
            //
            // if (serializer.TryReadDataField("component", out ComponentConstructionGraphStep component))
            // {
            //     return component;
            // }
            //
            // if (serializer.TryReadDataField("steps", out NestedConstructionGraphStep nested))
            // {
            //     return nested;
            // }

            throw new ArgumentException("Tried to convert invalid YAML node mapping to ConstructionGraphStep!");
        }

        public IDataNode TypeToNode(ConstructionGraphStep value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetMappingNode();
        }
    }
}
