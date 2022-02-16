using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared.Sound
{
    [TypeSerializer]
    public sealed class SoundSpecifierTypeSerializer :
        ITypeReader<SoundSpecifier, MappingDataNode>,
        ITypeReader<SoundSpecifier, ValueDataNode>
    {
        private Type GetType(MappingDataNode node)
        {
            var hasPath = node.Has(SoundPathSpecifier.Node);
            var hasCollection = node.Has(SoundCollectionSpecifier.Node);

            if (hasPath || !(hasPath ^ hasCollection))
                return typeof(SoundPathSpecifier);

            if (hasCollection)
                return typeof(SoundCollectionSpecifier);

            return typeof(SoundPathSpecifier);
        }

        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            var type = GetType(node);
            return serializationManager.Read(type, node, context, skipHook);
        }

        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            return new DeserializedValue<SoundSpecifier>(new SoundPathSpecifier(node.Value));
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            if (node.Has(SoundPathSpecifier.Node) && node.Has(SoundCollectionSpecifier.Node))
                return new ErrorNode(node, "You can only specify either a sound path or a sound collection!");

            if (!node.Has(SoundPathSpecifier.Node) && !node.Has(SoundCollectionSpecifier.Node))
                return new ErrorNode(node, "You need to specify either a sound path or a sound collection!");

            return serializationManager.ValidateNode(GetType(node), node, context);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            if (serializationManager.ValidateNode<ResourcePath>(node, context) is not ErrorNode)
                return new ValidatedValueNode(node);

            return new ErrorNode(node, "SoundSpecifier value is not a valid resource path!");
        }
    }
}
