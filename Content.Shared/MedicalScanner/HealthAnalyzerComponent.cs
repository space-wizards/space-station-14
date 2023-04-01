using Content.Shared.Disease;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.MedicalScanner;

[RegisterComponent]
public sealed class HealthAnalyzerComponent : Component
{
    /// <summary>
    /// How long it takes to scan someone.
    /// </summary>
    [DataField("scanDelay")]
    public float ScanDelay = 0.8f;

    /// <summary>
    ///     Sound played on scanning begin
    /// </summary>
    [DataField("scanningBeginSound")]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    ///     Sound played on scanning end
    /// </summary>
    [DataField("scanningEndSound")]
    public SoundSpecifier? ScanningEndSound;

    /// <summary>
    /// The disease this will give people.
    /// </summary>
    [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Disease;
}

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

[Serializable, NetSerializable]
public enum HealthAnalyzerUiKey : byte
{
    Key
}
