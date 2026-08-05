using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.MedicalScanner; //DS14
using Content.Shared.FixedPoint; //DS14

namespace Content.Server.Medical.Components;

/// <summary>
/// After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
/// <remarks>
/// Requires <c>ItemToggleComponent</c>.
/// </remarks>

//DS14-start
public record HealthAnalyzerData(
    string PatientName,
    Dictionary<string, FixedPoint2>? DamageDict,
    FixedPoint2 TotalDamage,
    List<HealthAnalyzerReagentEntry>? Reagents
);
//DS14-end

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(HealthAnalyzerSystem), typeof(CryoPodSystem))]
public sealed partial class HealthAnalyzerComponent : Component
{
    /// <summary>
    /// When should the next update be sent for the patient
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The delay between patient health updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// If the last state of the health analyzer was active (e.g. they are in range of the patient).
    /// </summary>
    [DataField]
    public bool IsAnalyzerActive = false;

    /// <summary>
    /// How long it takes to scan someone.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// Which entity has been scanned, for continuous updates
    /// </summary>
    [DataField]
    public EntityUid? ScannedEntity;

    /// <summary>
    /// The maximum range in tiles at which the analyzer can receive continuous updates, a value of null will be infinite range
    /// </summary>
    [DataField]
    public float? MaxScanRange = 2.5f;

    /// <summary>
    /// Sound played on scanning begin
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    /// Sound played on scanning end
    /// </summary>
    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Whether to show up the popup
    /// </summary>
    [DataField]
    public bool Silent;

    //DS14-start
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextPrintAllowed = TimeSpan.Zero;

    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    public HealthAnalyzerUiState? LastScannedState;

    public string? LastScannedName;

    public HealthAnalyzerData? LastScanData;
    //DS14-end
}
