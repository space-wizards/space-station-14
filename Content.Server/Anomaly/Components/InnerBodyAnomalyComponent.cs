using Content.Server.Anomaly.Effects;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// An anomaly within the body of a living being. Controls the ability to return to the standard state.
/// </summary>
[RegisterComponent, Access(typeof(InnerBodyAnomalySystem))]
public sealed partial class InnerBodyAnomalyComponent : Component
{
    /// <summary>
    /// All added by anomaly components. Should be removed after anomaly shutdown
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components;

    /// <summary>
    /// Local chat message to body owner
    /// </summary>
    [DataField]
    public LocId? StartMessage = null;

    /// <summary>
    /// Action in access of an entity
    /// </summary>
    [DataField]
    public EntityUid? Action = null;

    /// <summary>
    /// prototypes of the action that the entity will receive
    /// </summary>
    [DataField]
    public EntProtoId? ActionProto = "ActionAnomalyPulse";
}
