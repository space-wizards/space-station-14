#nullable enable
using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Content.Shared.Construction
{
    [TypeSerializer]
    public class ConstructionGraphStepTypeSerializer : ITypeSerializer<ConstructionGraphStep, MappingDataNode>
    {
        // TODO PAUL SERV3
        public ConstructionGraphStep Read(MappingDataNode node, ISerializationContext? context = null)
        {
            var serializationManager = IoCManager.Resolve<IServ3Manager>();

            if (node.HasNode("material"))
            {
                return serializationManager.ReadValue<MaterialConstructionGraphStep>(node);
            }

            if (node.HasNode("tool"))
            {
                return serializationManager.ReadValue<ToolConstructionGraphStep>(node);
            }

            if (node.HasNode("prototype"))
            {
                return serializationManager.ReadValue<PrototypeConstructionGraphStep>(node);
            }

            if (node.HasNode("component"))
            {
                return serializationManager.ReadValue<ComponentConstructionGraphStep>(node);
            }

            if (node.HasNode("tag"))
            {
                return serializationManager.ReadValue<TagConstructionGraphStep>(node);
            }

            if (node.HasNode("allTags") || node.HasNode("anyTags"))
            {
                return serializationManager.ReadValue<MultipleTagsConstructionGraphStep>(node);
            }

            if (node.HasNode("steps"))
            {
                return serializationManager.ReadValue<NestedConstructionGraphStep>(node);
            }

            throw new ArgumentException("Tried to convert invalid YAML node mapping to ConstructionGraphStep!");
        }

        public DataNode Write(ConstructionGraphStep value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new MappingDataNode();
        }
    }
}
