#nullable enable
using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Construction
{
    [TypeSerializer]
    public class ConstructionGraphStepTypeSerializer : ITypeReader<ConstructionGraphStep, MappingDataNode>
    {
        private Type? GetType(MappingDataNode node)
        {
            if (node.HasNode("material"))
            {
                return typeof(MaterialConstructionGraphStep);
            }
            else if (node.HasNode("tool"))
            {
                return typeof(ToolConstructionGraphStep);
            }
            else if (node.HasNode("prototype"))
            {
                return typeof(PrototypeConstructionGraphStep);
            }
            else if (node.HasNode("component"))
            {
                return typeof(ComponentConstructionGraphStep);
            }
            else if (node.HasNode("tag"))
            {
                return typeof(TagConstructionGraphStep);
            }
            else if (node.HasNode("allTags") || node.HasNode("anyTags"))
            {
                return typeof(MultipleTagsConstructionGraphStep);
            }
            else if (node.HasNode("steps"))
            {
                return typeof(NestedConstructionGraphStep);
            }
            else
            {
                return null;
            }
        }

        public DeserializationResult Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null)
        {
            var type = GetType(node) ??
                       throw new ArgumentException(
                           "Tried to convert invalid YAML node mapping to ConstructionGraphStep!");

            return serializationManager.Read(type, node, context, skipHook);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var type = GetType(node);

            if (type == null) return new ErrorNode(node, "No construction graph step type found.", true);

            return serializationManager.ValidateNode(type, node, context);
        }
    }
}
