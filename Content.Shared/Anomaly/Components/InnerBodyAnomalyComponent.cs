using Content.Shared.Anomaly.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// An anomaly within the body of a living being. Controls the ability to return to the standard state.
/// </summary>
[RegisterComponent, Access(typeof(SharedInnerBodyAnomalySystem))]
public sealed partial class InnerBodyAnomalyComponent : Component
{
    /// <summary>
    /// All added by anomaly components. Should be removed after anomaly shutdown
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

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

    /// <summary>
    /// The sprite to be added on the original entity. Allows you to visually identify the feature and type of anomaly to other players
    /// </summary>
    [DataField]
    public SpriteSpecifier? LayerSprite = null;

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted
    /// </summary>
    [DataField]
    public string LayerMap = "inner_anomaly_layer";
}
