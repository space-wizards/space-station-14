using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.HealthExaminable;

[RegisterComponent, Access(typeof(HealthExaminableSystem))]
public sealed partial class HealthExaminableComponent : Component
{
    /// <summary>
    /// Damage thresholds between examinable damage states.
    /// </summary>
    [DataField]
    public FixedPoint2[] Thresholds = [
        8,
        15,
        30,
        50,
        75,
        100,
        200
    ];

    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> ExaminableTypes = default!;

    /// <summary>
    ///     Health examine text is automatically generated through creating loc string IDs, in the form:
    ///     `health-examine-[prefix]-[type]-[threshold]`
    ///     This part determines the prefix.
    /// </summary>
    [DataField]
    public string LocPrefix = "carbon";
}
