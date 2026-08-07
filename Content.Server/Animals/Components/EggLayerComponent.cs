using Content.Server.Animals.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Animals.Components;

/// <summary>
/// Defines the sound and player action for an egg-laying entity.
/// Timing and hunger consumption are configured by <see cref="HungerProductionComponent"/>.
/// The egg itself is configured by <see cref="EntityProducerComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(EggLayerSystem))]
public sealed partial class EggLayerComponent : Component
{
    /// <summary>
    ///     Player action.
    /// </summary>
    [DataField]
    public EntProtoId EggLayAction = "ActionAnimalLayEgg";

    [DataField]
    public SoundSpecifier EggLaySound = new SoundPathSpecifier("/Audio/Effects/pop.ogg");

    [DataField]
    public EntityUid? Action;
}
