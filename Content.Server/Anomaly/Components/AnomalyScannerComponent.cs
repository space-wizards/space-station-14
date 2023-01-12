using System.Threading;

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

    [DataField("scanDoAfterDuration")]
    public float ScanDoAfterDuration = 5;

    public CancellationTokenSource? TokenSource;
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
