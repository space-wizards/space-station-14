using System.Diagnostics.CodeAnalysis;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared.Maps;

public sealed partial class MapResPathSerializer : ITypeSerializer<ResPath, ValueDataNode>
{
    [Dependency] private IResourceManager _resource = default!;

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var path = ResPath.FromRelativeSystemPath(node.Value);

        if (path.Extension != MapLoaderSystem.SaveExtension && path.Extension != "yml")
            return new ErrorNode(node, $"Unsupported file extension for a map file path: {path.Extension.ToUpper()}");

        if (TryGetMapResPath(path, out var newPath))
            return new ValidatedValueNode(new ValueDataNode(newPath.ToString()));

        return new ErrorNode(node, "Failed to find a map with the specified path!");
    }

    public ResPath Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ResPath>? instanceProvider = null)
    {
        var path = ResPath.FromRelativeSystemPath(node.Value);

        return TryGetMapResPath(path, out var newPath)
            ? newPath.Value
            : serializationManager.Read(node, hookCtx, context, instanceProvider);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        ResPath value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value);
    }

    private bool TryGetMapResPath(ResPath resPath, [NotNullWhen(true)] out ResPath? newPath)
    {
        resPath = resPath.ToRootedPath();

        if (_resource.ContentFileExists(resPath))
        {
            newPath = resPath;
            return true;
        }

        var oppositePath = resPath.WithExtension(
            resPath.Extension == MapLoaderSystem.SaveExtension
                ? "yml"
                : MapLoaderSystem.SaveExtension);

        if (_resource.ContentFileExists(oppositePath))
        {
            newPath = oppositePath;
            return true;
        }

        newPath = null;
        return false;
    }
}
