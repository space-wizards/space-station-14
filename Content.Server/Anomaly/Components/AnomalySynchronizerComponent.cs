using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// a device that allows you to translate anomaly activity into multitool signals.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(AnomalySynchronizerSystem))]
public sealed partial class AnomalySynchronizerComponent : Component
{
    /// <summary>
    /// The uid of the anomaly to which the synchronizer is connected.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedAnomaly;

    /// <summary>
    /// Should the anomaly pulse when connected to the synchronizer?
    /// </summary>
    [DataField]
    public bool PulseOnConnect = true;

    /// <summary>
    /// Should the anomaly pulse when disconnected from synchronizer?
    /// </summary>
    [DataField]
    public bool PulseOnDisconnect = false;

    /// <summary>
    /// minimum distance from the synchronizer to the anomaly to be attached
    /// </summary>
    [DataField]
    public float AttachRange = 0.4f;

    /// <summary>
    /// Periodicheski checks to see if the anomaly has moved to disconnect it.
    /// </summary>
    [DataField]
    public TimeSpan CheckFrequency = TimeSpan.FromSeconds(1f);

    [DataField, AutoPausedField]
    public TimeSpan NextCheckTime = TimeSpan.Zero;

    [DataField]
    public ProtoId<SourcePortPrototype> DecayingPort = "Decaying";

    [DataField]
    public ProtoId<SourcePortPrototype> StabilizePort = "Stabilize";

    [DataField]
    public ProtoId<SourcePortPrototype> GrowingPort = "Growing";

    [DataField]
    public ProtoId<SourcePortPrototype> PulsePort = "Pulse";

    [DataField]
    public ProtoId<SourcePortPrototype> SupercritPort = "Supercritical";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ConnectedSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier DisconnectedSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");
}
