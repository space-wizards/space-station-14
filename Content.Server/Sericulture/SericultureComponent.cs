using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Sericulture;

[RegisterComponent]
public sealed partial class SericultureComponent : Component
{
    [DataField("popupText")]
    public string PopupText = "sericulture-failure-hunger";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField("entityProduced", required: true)]
    public string EntityProduced = "";

    [DataField("actionProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionProto = "ActionSericulture";

    [DataField("action")] public EntityUid? Action;

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField("productionLength", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float ProductionLength = 0;

    [DataField("hungerCost"), ViewVariables(VVAccess.ReadWrite)]
    public float HungerCost = 0f;
}
