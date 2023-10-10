using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// 
/// </summary>
[RegisterComponent, Access(typeof(AnomalySynchronizerSystem))]
public sealed partial class AnomalySynchronizerComponent : Component
{
    /// <summary>
    /// The uid of the anomaly to which the synchronizer is connected.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid ConnectedAnomaly;


    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> DecayingPort = "Decaying";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> NormalizePort = "Normalize";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> GrowingPort = "Growing";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> PulsePort = "Pulse";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> SupercritPort = "Supercrit";
}
