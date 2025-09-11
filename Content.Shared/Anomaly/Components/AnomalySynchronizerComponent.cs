using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// A device that allows you to translate anomaly activity into multitool signals.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AnomalySynchronizerSystem))]
public sealed partial class AnomalySynchronizerComponent : Component
{
    /// <summary>
    /// The uid of the anomaly to which the synchronizer is connected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedAnomaly;

    /// <summary>
    /// Should the anomaly pulse when connected to the synchronizer?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PulseOnConnect = true;

    /// <summary>
    /// Should the anomaly pulse when disconnected from synchronizer?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PulseOnDisconnect = false;

    /// <summary>
    /// Minimum distance from the synchronizer to the anomaly to be attached.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AttachRange = 0.4f;

    /// <summary>
    /// Periodically checks to see if the anomaly has moved to disconnect it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CheckFrequency = TimeSpan.FromSeconds(1f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
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

    [DataField]
    public SoundSpecifier ConnectedSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");

    [DataField]
    public SoundSpecifier DisconnectedSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");
}
