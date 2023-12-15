using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.UserInterface;

[RegisterComponent]
public sealed partial class IntrinsicUIComponent : Component
{
    /// <summary>
    /// List of UIs and their actions that this entity has.
    /// </summary>
    [DataField("uis", required: true)] public List<IntrinsicUIEntry> UIs = new();
}

[DataDefinition]
public partial class IntrinsicUIEntry
{
    /// <summary>
    /// The BUI key that this intrinsic UI should open.
    /// </summary>
    [DataField("key", required: true)]
    public Enum? Key { get; private set; }

    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string? ToggleAction;

    /// <summary>
    /// The action used for this BUI.
    /// </summary>
    [DataField("toggleActionEntity")]
    public EntityUid? ToggleActionEntity = new();
}
