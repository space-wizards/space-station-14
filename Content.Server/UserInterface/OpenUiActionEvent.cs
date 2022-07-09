using Content.Shared.Actions;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.UserInterface;

public sealed class OpenUiActionEvent : InstantActionEvent, ISerializationHooks
{
    [ViewVariables]
    public Enum? Key { get; set; }

    [DataField("key", readOnly: true, required: true)]
    private string _keyRaw = default!;

    void ISerializationHooks.AfterDeserialization()
    {
        var reflectionManager = IoCManager.Resolve<IReflectionManager>();
        if (reflectionManager.TryParseEnumReference(_keyRaw, out var key))
            Key = key;
        else
            Logger.Error($"Invalid UI key ({_keyRaw}) in open-UI action");
    }
}
