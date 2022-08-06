using Content.Server.Fluids.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Access(typeof(SpraySystem))]
public sealed class SprayComponent : Component
{
    public const string SolutionName = "spray";

    [DataField("sprayDistance")] public float SprayDistance = 3f;

    [DataField("transferAmount")] public FixedPoint2 TransferAmount = FixedPoint2.New(10);

    [DataField("sprayVelocity")] public float SprayVelocity = 1.5f;

    [DataField("sprayAliveTime")] public float SprayAliveTime = 0.75f;

    [DataField("cooldownTime")] public float CooldownTime = 0.5f;

    [DataField("sprayedPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SprayedPrototype = "Vapor";

    [DataField("vaporAmount")] public int VaporAmount = 1;

    [DataField("vaporSpread")] public float VaporSpread = 90f;

    [DataField("impulse")] public float Impulse;

    [DataField("spraySound", required: true)]
    [Access(typeof(SpraySystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public SoundSpecifier SpraySound { get; } = default!;
}
