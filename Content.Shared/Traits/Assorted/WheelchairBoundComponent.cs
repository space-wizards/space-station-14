using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Traits.Assorted;

[RegisterComponent, NetworkedComponent]
public sealed class WheelchairBoundComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("carriagePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CarriagePrototype = "Carriage";
}
