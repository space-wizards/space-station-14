using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This is used for scanning anomalies and
/// displaying information about them in the ui
/// </summary>
[RegisterComponent]
public sealed class AnomalyScannerComponent : Component
{
    /// <summary>
    /// The anomaly that was last scanned by this scanner.
    /// </summary>
    [ViewVariables]
    public EntityUid? ScannedAnomaly;

    /// <summary>
    /// How long the scan takes
    /// </summary>
    [DataField("scanDoAfterDuration")]
    public float ScanDoAfterDuration = 5;

    public CancellationTokenSource? TokenSource;

    /// <summary>
    /// The sound plays when the scan finished
    /// </summary>
    [DataField("completeSound")]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");
}

public sealed class AnomalyScanFinishedEvent : EntityEventArgs
{
    public EntityUid Anomaly;

    public EntityUid User;

    public AnomalyScanFinishedEvent(EntityUid anomaly, EntityUid user)
    {
        Anomaly = anomaly;
        User = user;
    }
}

public sealed class AnomalyScanCancelledEvent : EntityEventArgs
{
}
