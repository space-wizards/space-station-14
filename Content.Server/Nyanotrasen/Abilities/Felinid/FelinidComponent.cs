using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.Utility;

namespace Content.Server.Abilities.Felinid;

[RegisterComponent]
public sealed partial class FelinidComponent : Component
{
    /// <summary>
    /// The hairball prototype to use.
    /// </summary>
    [DataField("hairballPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HairballPrototype = "Hairball";

    //[DataField("hairballAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    //public string HairballAction = "ActionHairball";

    [DataField("hairballActionId",
        customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? HairballActionId = "ActionHairball";

    [DataField("hairballAction")]
    public EntityUid? HairballAction;

    [DataField("eatActionId",
        customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EatActionId = "ActionEatMouse";

    [DataField("eatAction")]
    public EntityUid? EatAction;

    [DataField("eatActionTarget")]
    public EntityUid? EatActionTarget = null;
}
