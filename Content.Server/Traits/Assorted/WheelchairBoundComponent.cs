using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// On adding spawns wheelchair prototype and tries buckle player to it, then self removing
/// </summary>
[RegisterComponent, Access(typeof(WheelchairBoundSystem))]
public sealed partial class WheelchairBoundComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("wheelchairPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WheelchairPrototype = "VehicleWheelchair";
}
