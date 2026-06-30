using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Nutrition.Prototypes;

[UsedImplicitly, TypeSerializer]
public sealed class SatiationValueSerializer : ITypeSerializer<SatiationValue, ValueDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    ) => int.TryParse(node.Value, out _)
        ? serializationManager.ValidateNode<int>(node, context)
        : serializationManager.ValidateNode<string>(node, context);

    public SatiationValue Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SatiationValue>? instanceProvider = null
    ) => int.TryParse(node.Value, out _)
        ? serializationManager.Read<int>(node, context)
        : serializationManager.Read<string>(node, context, notNullableOverride: true);

    public DataNode Write(
        ISerializationManager serializationManager,
        SatiationValue value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    ) => value.Key is not null
        ? serializationManager.WriteValue(value.Key, notNullableOverride: true)
        : serializationManager.WriteValue(value.Value);
}
