#nullable enable
using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Content.Shared.Construction
{
    [TypeSerializer]
    public class ConstructionGraphStepTypeSerializer : ITypeReader<ConstructionGraphStep, MappingDataNode>
    {
        // TODO PAUL SERV3
        public DeserializationResult Read(ISerializationManager serializationManager,
            MappingDataNode node,
            ISerializationContext? context = null)
        {
            Type type;

            if (node.HasNode("material"))
            {
                type = typeof(MaterialConstructionGraphStep);
            }
            else if (node.HasNode("tool"))
            {
                type = typeof(ToolConstructionGraphStep);
            }
            else if (node.HasNode("prototype"))
            {
                type = typeof(PrototypeConstructionGraphStep);
            }
            else if (node.HasNode("component"))
            {
                type = typeof(ComponentConstructionGraphStep);
            }
            else if (node.HasNode("tag"))
            {
                type = typeof(TagConstructionGraphStep);
            }
            else if (node.HasNode("allTags") || node.HasNode("anyTags"))
            {
                type = typeof(MultipleTagsConstructionGraphStep);
            }
            else if (node.HasNode("steps"))
            {
                type = typeof(NestedConstructionGraphStep);
            }
            else
            {
                throw new ArgumentException("Tried to convert invalid YAML node mapping to ConstructionGraphStep!");
            }

            return new DeserializedValue<ConstructionGraphStep>(serializationManager.ReadValueOrThrow<ConstructionGraphStep>(type, node));
        }
    }
}
