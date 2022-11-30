using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Client.UserInterface.Fragments;

/// <summary>
/// Boilerplate serializer for defining the ui fragment in yaml
/// </summary>
/// <example>
/// This is an example from the yaml definition from the notekeeper cartridge ui
/// <code>
/// - type: CartridgeUi
///     ui: !type:NotekeeperUi
/// </code>
/// </example>
public sealed class UIFragmentSerializer : ITypeSerializer<UIFragment, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<UIFragment>(node, context);
    }

    public UIFragment Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, UIFragment? value = default)
    {
        return serializationManager.Read(node, context, skipHook, value);
    }

    public UIFragment Copy(ISerializationManager serializationManager, UIFragment source, UIFragment target, bool skipHook,
        ISerializationContext? context = null)
    {
        return serializationManager.Copy(source, context, skipHook);
    }

    public DataNode Write(ISerializationManager serializationManager, UIFragment value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value, alwaysWrite, context);
    }
}
