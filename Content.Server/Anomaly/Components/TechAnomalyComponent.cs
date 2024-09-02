using Content.Server.Anomaly.Effects;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Components;

[RegisterComponent, Access(typeof(TechAnomalySystem))]
public sealed partial class TechAnomalyComponent : Component
{
    /// <summary>
    /// the distance at which random ports will bind to the anomaly. Scales with severity.
    /// </summary>
    [DataField]
    public MinMax LinkRadius = new(3, 10);

    /// <summary>
    /// the maximum number of entities with which an anomaly is associated during pulsing. Scales with severity
    /// </summary>
    [DataField]
    public MinMax LinkCountPerPulse = new(1, 5);

    /// <summary>
    /// Number of linkable pairs. when supercrit, the anomaly will link random devices in the radius to each other in pairs.
    /// </summary>
    [DataField]
    public int LinkCountSupercritical = 30;

    /// <summary>
    /// port activated by pulsation of the anomaly
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> PulsePort = "Pulse";

    /// <summary>
    /// A port that activates every few seconds of an anomaly's lifetime
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> TimerPort = "Timer";

    /// <summary>
    /// A prototype beam shot into devices when pulsed
    /// </summary>
    [DataField]
    public EntProtoId LinkBeamProto = "AnomalyTechBeam";
}
