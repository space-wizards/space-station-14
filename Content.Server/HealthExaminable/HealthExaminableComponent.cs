using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.HealthExaminable;

[RegisterComponent, Access(typeof(HealthExaminableSystem))]
public sealed class HealthExaminableComponent : Component
{
    public List<FixedPoint2> Thresholds = new()
        { FixedPoint2.New(10), FixedPoint2.New(25), FixedPoint2.New(50), FixedPoint2.New(75) };

    [DataField("examinableDamages", required: true)]
    public ExaminableDamageSpecifier ExaminableDamageSpecifiers = default!;

    /// <summary>
    ///     Health examine text is automatically generated through creating loc string IDs, in the form:
    ///     `health-examine-[prefix]-[type]-[threshold]`
    ///     This part determines the prefix.
    /// </summary>
    [DataField("locPrefix")]
    public string LocPrefix = "carbon";
}

[DataDefinition]
public sealed class ExaminableDamageSpecifier
{
    [DataField("types", readOnly: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DamageTypePrototype>))]
    public HashSet<string> Types { get; set; } = new();
    [DataField("groups", readOnly: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DamageGroupPrototype>))]
    public HashSet<string> Groups { get; set; } = new();
}
