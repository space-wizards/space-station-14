using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(VomitOnDamageSystem))]
public sealed partial class VomitOnDamageComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2?> Damage;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public TimeSpan VomitCooldown = TimeSpan.FromMinutes(5);

    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextVomitTime = TimeSpan.Zero;
}
