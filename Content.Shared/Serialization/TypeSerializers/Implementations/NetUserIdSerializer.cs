using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using System;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class NetUserIdSerializer : ITypeSerializer<NetUserId, ValueDataNode>, ITypeCopyCreator<NetUserId>
    {
        public NetUserId Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<NetUserId>? instanceProvider = null)
        {
            var val = Guid.Parse(node.Value);
            return new NetUserId(val);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return Parse.TryInt32(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing NetUserId");
        }

        public DataNode Write(ISerializationManager serializationManager, NetUserId value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        [MustUseReturnValue]
        public NetUserId CreateCopy(ISerializationManager serializationManager, NetUserId source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            var val = Guid.Parse(source.ToString());
            return new(val);
        }
    }
}
