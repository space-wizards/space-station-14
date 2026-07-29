using Content.Server.Animals.Systems;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Animals.Components;

/// <summary>
/// Defines the product, sound, and player action for an egg-laying entity.
/// Timing and hunger consumption are configured by <see cref="HungerProductionComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(EggLayerSystem))]
public sealed partial class EggLayerComponent : Component
{
    /// <summary>
    ///     The item that gets laid/spawned, retrieved from animal prototype.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> EggSpawn = new();

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
