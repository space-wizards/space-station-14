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
    public LocId TooHungryPopup = "action-popup-lay-egg-too-hungry";

    [DataField]
    public LocId UserPopup = "action-popup-lay-egg-user";

    [DataField]
    public LocId OthersPopup = "action-popup-lay-egg-others";
}
