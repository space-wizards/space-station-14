using Robust.Shared.Serialization;
using Content.Shared.FixedPoint;

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly EntityUid? TargetEntity;
    public float Temperature;
    public float BloodLevel;

    public Dictionary<string, FixedPoint2>? Chemicals;

    public HealthAnalyzerScannedUserMessage(EntityUid? targetEntity, float temperature, float bloodLevel, Dictionary<string, FixedPoint2>? chemicals= null)
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        Chemicals = chemicals;
    }
}

