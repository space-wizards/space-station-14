using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Alert;

public sealed class AlertKeySerializer : ITypeSerializer<AlertKey, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (!node.TryGetAndValidate<AlertType>("id", serializationManager, context, out var idValidationNode))
            return idValidationNode;
        if (!node.TryGetAndValidate<AlertType>("category", serializationManager, context, out var categoryValidationNode))
            return categoryValidationNode;
        if (serializationManager.Read<AlertType>(node["id"], context) == AlertType.Error)
            return new ErrorNode(node, "missing or invalid alertType for alert");
        return new ValidatedValueNode(node);
    }

    public AlertKey Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, AlertKey value = default)
    {
        AlertType? alertType = null;
        if (node.TryGet("id", out var idNode))
            alertType = serializationManager.Read<AlertType>(idNode);

        AlertCategory? alertCategory = null;
        if (node.TryGet("category", out var categoryNode))
            alertCategory = serializationManager.Read<AlertCategory>(categoryNode);

        if (alertType == AlertType.Error)
        {
            Logger.ErrorS("alert", "missing or invalid alertType for alert");
        }

        return new AlertKey(alertType, alertCategory);
    }

    public DataNode Write(ISerializationManager serializationManager, AlertKey value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public AlertKey Copy(ISerializationManager serializationManager, AlertKey source, AlertKey target, bool skipHook,
        ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }
}
