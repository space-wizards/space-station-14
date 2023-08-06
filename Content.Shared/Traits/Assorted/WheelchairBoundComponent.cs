using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Traits.Assorted;

[RegisterComponent, NetworkedComponent]
public sealed class WheelchairBoundComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("wheelchairPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WheelchairPrototype = "Wheelchair";
}
