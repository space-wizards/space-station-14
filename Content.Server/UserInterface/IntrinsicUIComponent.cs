using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.UserInterface;

[RegisterComponent]
public sealed class IntrinsicUIComponent : Component, ISerializationHooks
{
    [ViewVariables, DataField("uis", required: true)]
    public List<IntrinsicUIEntry> UIs = new();

    void ISerializationHooks.AfterDeserialization()
    {
        foreach (var ui in UIs)
        {
            ui.AfterDeserialization();
        }
    }
}

[DataDefinition]
public struct IntrinsicUIEntry
{
    [ViewVariables]
    public Enum? Key { get; set; }

    [DataField("key", readOnly: true, required: true)]
    private string _keyRaw = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("adminOnly")]
    public bool AdminOnly { get; set; } = false;

    [DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();

    public void AfterDeserialization()
    {
        var reflectionManager = IoCManager.Resolve<IReflectionManager>();
        if (reflectionManager.TryParseEnumReference(_keyRaw, out var key))
            Key = key;

        if (ToggleAction.Event is ToggleIntrinsicUIEvent ev)
        {
            ev.Key = Key;
        }
    }
}
