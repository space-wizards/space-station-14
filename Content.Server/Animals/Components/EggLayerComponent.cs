using Content.Server.Animals.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Animals.Components;

/// <summary>
/// Defines the sound for an egg-laying entity.
/// Timing and hunger consumption are configured by <see cref="HungerProductionComponent"/>.
/// The egg itself is configured by <see cref="EntityProducerComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(EggLayerSystem))]
public sealed partial class EggLayerComponent : Component
{
    [DataField]
    public SoundSpecifier EggLaySound = new SoundPathSpecifier("/Audio/Effects/pop.ogg");
}
