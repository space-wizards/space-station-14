using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.UserInterface;

[RegisterComponent]
public sealed partial class IntrinsicUIComponent : Component
{
    /// <summary>
    /// List of UIs and their actions that this entity has.
    /// </summary>
    [DataField("uis", required: true)] public Dictionary<Enum, IntrinsicUIEntry> UIs = new();
}

[DataDefinition]
public sealed partial class IntrinsicUIEntry
{
    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string? ToggleAction;

    /// <summary>
    /// The action used for this BUI.
    /// </summary>
    [DataField("toggleActionEntity")]
    public EntityUid? ToggleActionEntity = new();
}
