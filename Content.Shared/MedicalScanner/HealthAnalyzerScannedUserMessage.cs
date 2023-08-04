using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly EntityUid? TargetEntity;

    public HealthAnalyzerScannedUserMessage(EntityUid? targetEntity)
    {
        TargetEntity = targetEntity;
    }
}

