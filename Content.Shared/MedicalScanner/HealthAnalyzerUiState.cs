using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

[Serializable, NetSerializable]
public sealed class HealthAnalyzerUiState : BoundUserInterfaceState
{
    public string EntityName = "Unknown";

    public float Temperature;
    public float BloodLevel;

    public DamageDetails? Damage;

    public HealthAnalyzerUiState(HealthAnalyzerScannedUserMessage msg)
    {
        var entities = IoCManager.Resolve<IEntityManager>();

        Temperature = msg.Temperature;
        BloodLevel = msg.BloodLevel;

        if (msg.TargetEntity != null &&
            entities.TryGetComponent<MetaDataComponent>(msg.TargetEntity.Value, out var metaData))
            EntityName = Identity.Name(msg.TargetEntity.Value, entities);

        if (msg.TargetEntity == null ||
            !entities.TryGetComponent<DamageableComponent>(msg.TargetEntity, out var damageable))
            return;

        Damage = new DamageDetails(damageable.TotalDamage, damageable.DamagePerGroup, damageable.Damage.DamageDict);
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class DamageDetails
{
    public FixedPoint2 TotalDamage;
    public Dictionary<string, FixedPoint2> DamagePerGroup;
    public Dictionary<string, FixedPoint2> DamagePerType;

    public DamageDetails(FixedPoint2 totalDamage, Dictionary<string, FixedPoint2> damagePerGroup, Dictionary<string, FixedPoint2> damagePerType)
    {
        TotalDamage = totalDamage;
        DamagePerGroup = damagePerGroup;
        DamagePerType = damagePerType;
    }
}
