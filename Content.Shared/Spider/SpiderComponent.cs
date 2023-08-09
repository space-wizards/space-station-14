using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed class SpiderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("webPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WebPrototype = "SpiderWeb";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("webActionName")]
    public string WebActionName = "SpiderWebAction";
}

public sealed class SpiderWebActionEvent : InstantActionEvent { }
