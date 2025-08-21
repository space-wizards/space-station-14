using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.Components;

/// <summary>
/// Makes an entity vomit after receiving certain damage types.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(VomitOnDamageSystem))]
public sealed partial class VomitOnDamageComponent : Component
{
    /// <summary>
    /// Key - damage type. Value - damage threshold.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2?> Damage;

    /// <summary>
    /// Cooldown between vomits.
    /// </summary>
    [DataField]
    public TimeSpan VomitCooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Next time when an entity will be able to vomit.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextVomitTime = TimeSpan.Zero;
}
