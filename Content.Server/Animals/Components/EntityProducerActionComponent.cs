using Content.Server.Animals.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Animals.Components;

/// <summary>
/// Defines action feedback for an entity producer.
/// </summary>
[RegisterComponent, Access(typeof(EntityProducerActionSystem))]
public sealed partial class EntityProducerActionComponent : Component
{
    /// <summary>
    /// Sound played after entities are successfully produced.
    /// </summary>
    [DataField]
    public SoundSpecifier ProductionSound = new SoundPathSpecifier("/Audio/Effects/pop.ogg");

    /// <summary>
    /// Popup shown to the acting entity when production fails due to insufficient configured satiation.
    /// </summary>
    [DataField]
    public LocId InsufficientSatiationPopup = "entity-producer-action-popup-too-hungry";

    /// <summary>
    /// Feedback shown to the producer after successful entity production.
    /// </summary>
    [DataField]
    public LocId UserPopup = "entity-producer-action-popup-user";

    /// <summary>
    /// Feedback shown to nearby observers after successful entity production.
    /// </summary>
    [DataField]
    public LocId OthersPopup = "entity-producer-action-popup-others";
}
