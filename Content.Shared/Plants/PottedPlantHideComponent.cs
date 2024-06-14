using Content.Shared.Storage.Components;
using Robust.Shared.Audio;

namespace Content.Shared.Plants
{
    /// <summary>
    ///     Interaction wrapper for <see cref="SecretStashComponent"/>.
    ///     Gently rustle after each interaction with plant.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(PottedPlantHideSystem))]
    public sealed partial class PottedPlantHideComponent : Component
    {
        [DataField("rustleSound")]
        public SoundSpecifier RustleSound = new SoundPathSpecifier("/Audio/Effects/plant_rustle.ogg");
    }
}
