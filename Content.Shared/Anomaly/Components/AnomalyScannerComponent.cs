using Content.Shared.Anomaly;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This is used for scanning anomalies and
/// displaying information about them in the ui
/// </summary>
[RegisterComponent, Access(typeof(SharedAnomalyScannerSystem))]
[NetworkedComponent]
public sealed partial class AnomalyScannerComponent : Component
{
    /// <summary>
    /// The anomaly that was last scanned by this scanner.
    /// </summary>
    [ViewVariables]
    public EntityUid? ScannedAnomaly;

    /// <summary>
    /// How long the scan takes
    /// </summary>
    [DataField]
    public float ScanDoAfterDuration = 5;

    /// <summary>
    /// The sound plays when the scan finished
    /// </summary>
    [DataField]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");

    /// <summary>
    /// Whether to ignore the secret data on the anomaly.
    /// </summary>
    [DataField]
    public bool IgnoreSecret;
}
