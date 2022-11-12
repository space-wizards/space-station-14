using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
namespace Content.Shared.Medical.Wounds.Prototypes;

[DataDefinition]
public sealed class WoundPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    [DataField("damageToApply")] public DamageSpecifier DamageToApply { get; init; } = new();
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundData (string WoundId, float Severity, float Tended, float Size, float Infected);
