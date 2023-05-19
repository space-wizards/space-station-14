using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Access(typeof(SpraySystem))]
public sealed class SprayComponent : Component
{
    public const string SolutionName = "spray";

    [ViewVariables(VVAccess.ReadWrite), DataField("sprayDistance")]
    public float SprayDistance = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("sprayVelocity")]
    public float SprayVelocity = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("sprayedPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SprayedPrototype = "Vapor";

    [ViewVariables(VVAccess.ReadWrite), DataField("vaporAmount")]
    public int VaporAmount = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("vaporSpread")]
    public float VaporSpread = 90f;

    [ViewVariables(VVAccess.ReadWrite), DataField("spraySound", required: true)]
    [Access(typeof(SpraySystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public SoundSpecifier SpraySound { get; } = default!;
}
