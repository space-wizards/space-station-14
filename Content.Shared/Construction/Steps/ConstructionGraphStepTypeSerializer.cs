using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Construction.Steps
{
    [TypeSerializer]
    public sealed class ConstructionGraphStepTypeSerializer : ITypeReader<ConstructionGraphStep, MappingDataNode>
    {
        private Type? GetType(MappingDataNode node)
        {
            if (node.Has("material"))
            {
                return typeof(MaterialConstructionGraphStep);
            }

            if (node.Has("tool"))
            {
                return typeof(ToolConstructionGraphStep);
            }

            if (node.Has("component"))
            {
                return typeof(ComponentConstructionGraphStep);
            }

            if (node.Has("tag"))
            {
                return typeof(TagConstructionGraphStep);
            }

            if (node.Has("allTags") || node.Has("anyTags"))
            {
                return typeof(MultipleTagsConstructionGraphStep);
            }

            if (node.Has("minTemperature") || node.Has("maxTemperature"))
            {
                return typeof(TemperatureConstructionGraphStep);
            }

            if (node.Has("assemblyId") || node.Has("guideString"))
            {
                return typeof(PartAssemblyConstructionGraphStep);
            }

            // See Read below if you are adding new types
            return null;
        }

        public ConstructionGraphStep Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ConstructionGraphStep>? instanceProvider = null)
        {
            var type = GetType(node) ??
                       throw new ArgumentException(
                           "Tried to convert invalid YAML node mapping to ConstructionGraphStep!");

            if (type == typeof(MaterialConstructionGraphStep))
                return serializationManager.Read<MaterialConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            if (type == typeof(ToolConstructionGraphStep))
                return serializationManager.Read<ToolConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            if (type == typeof(ComponentConstructionGraphStep))
                return serializationManager.Read<ComponentConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            if (type == typeof(TagConstructionGraphStep))
                return serializationManager.Read<TagConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            if (type == typeof(MultipleTagsConstructionGraphStep))
                return serializationManager.Read<MultipleTagsConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            if (type == typeof(TemperatureConstructionGraphStep))
                return serializationManager.Read<TemperatureConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            if (type == typeof(PartAssemblyConstructionGraphStep))
                return serializationManager.Read<PartAssemblyConstructionGraphStep>(node, hookCtx, context, notNullableOverride: true);

            // See GetType above if you are adding new types
            throw new NotImplementedException();
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var type = GetType(node);

            if (type == null)
                return new ErrorNode(node, "No construction graph step type found.");

            return serializationManager.ValidateNode(type, node, context);
        }
    }
}
