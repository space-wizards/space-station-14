using Content.Server.Animals.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Animals.Components;

/// <summary>
/// Defines action feedback for an entity producer.
/// </summary>
[RegisterComponent, Access(typeof(EntityProducerActionSystem))]
public sealed partial class EntityProducerActionComponent : Component
{
    [DataField]
    public SoundSpecifier ProductionSound = new SoundPathSpecifier("/Audio/Effects/pop.ogg");

    [DataField]
    public LocId InsufficientSatiationPopup = "entity-producer-action-popup-too-hungry";

    [DataField]
    public LocId UserPopup = "entity-producer-action-popup-user";

    [DataField]
    public LocId OthersPopup = "entity-producer-action-popup-others";
}
