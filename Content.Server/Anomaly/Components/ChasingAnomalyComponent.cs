using Content.Server.Anomaly.Effects;
using Robust.Shared.Audio;
using Content.Shared.Chemistry.Components;
using System.Numerics;

namespace Content.Server.Anomaly.Components;

[RegisterComponent, Access(typeof(ChasingAnomalySystem))]
public sealed partial class ChasingAnomalyComponent : Component
{
    /// <summary>
    /// the maximum radius in which the anomaly chooses the creature to move to
    /// scales with stability
    /// </summary>
    [DataField("maxChaseRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxChaseRadius = 15;

    /// <summary>
    /// the speed at which the anomaly is moving towards the target.
    /// </summary>
    [DataField("chasingSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float ChasingSpeed = 0.5f;

    /// <summary>
    /// modification of the pursuit speed during the transition to a supercritical state
    /// </summary>
    [DataField("superCriticalSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float SuperCriticalSpeedModifier = 5;

    /// <summary>
    /// the component that the anomaly is chasing
    /// </summary>
    [DataField("chasingComponent", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string ChasingComponent = default!;

    //In Game Storage Variables

    /// <summary>
    /// the point where the anomaly seeks to reach
    /// </summary>
    [DataField("chasingEntity"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid ChasingEntity = default!;
}
