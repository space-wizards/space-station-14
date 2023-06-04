using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Traits.Assorted;

[RegisterComponent, Access(typeof(BrokenLegsSystem))]
public sealed class BrokenLegsComponent : Component
{
    [DataField("walkSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public int WalkSpeedModifier = 0;

    [DataField("sprintSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public int SprintSpeedModifier = 0;

    [DataField("accelerationSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public int AccelerationSpeedModifier = 0;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("carriageId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CarriageId = "Carriage";
}
