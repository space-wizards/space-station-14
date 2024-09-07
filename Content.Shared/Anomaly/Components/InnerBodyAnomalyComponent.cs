using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// An anomaly within the body of a living being. Controls the ability to return to the standard state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedInnerBodyAnomalySystem))]
public sealed partial class InnerBodyAnomalyComponent : Component
{
    /// <summary>
    /// A prototype of an entity whose components will be added to the anomaly host **AND** then removed at the right time
    /// </summary>
    [DataField(required: true)]
    public EntProtoId? InjectionProto;

    /// <summary>
    /// Duration of stun from the effect of the anomaly
    /// </summary>
    [DataField]
    public float StunDuration = 4f;

    /// <summary>
    /// A message sent in chat to a player who has become infected by an anomaly
    /// </summary>
    [DataField]
    public LocId? StartMessage = null;

    /// <summary>
    /// A message sent in chat to a player who has cleared an anomaly
    /// </summary>
    [DataField]
    public LocId? EndMessage = "inner-anomaly-end-message";

    /// <summary>
    /// Sound, playing on becoming anomaly
    /// </summary>
    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/Effects/inneranomaly.ogg");

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
    /// The fallback sprite to be added on the original entity. Allows you to visually identify the feature and type of anomaly to other players
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? FallbackSprite = null;

    /// <summary>
    /// Ability to use unique sprites for different body types
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<BodyPrototype>, SpriteSpecifier> SpeciesSprites = new();

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted
    /// </summary>
    [DataField]
    public string LayerMap = "inner_anomaly_layer";
}
