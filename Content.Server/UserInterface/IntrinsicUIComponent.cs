using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.UserInterface;

[RegisterComponent]
public sealed class IntrinsicUIComponent : Component, ISerializationHooks
{
    /// <summary>
    /// List of UIs and their actions that this entity has.
    /// </summary>
    [DataField("uis", required: true)]
    public List<IntrinsicUIEntry> UIs = new();

    void ISerializationHooks.AfterDeserialization()
    {
        for (var i = 0; i < UIs.Count; i++)
        {
            var ui = UIs[i];
            ui.AfterDeserialization();
            UIs[i] = ui;
        }
    }
}

[DataDefinition]
public struct IntrinsicUIEntry
{
    [ViewVariables] public Enum? Key { get; set; } = null;

    /// <summary>
    /// The BUI key that this intrinsic UI should open.
    /// </summary>
    [DataField("key", readOnly: true, required: true)]
    private string _keyRaw = default!;

    /// <summary>
    /// The action used for this BUI.
    /// </summary>
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

    public IntrinsicUIEntry() {}
}
