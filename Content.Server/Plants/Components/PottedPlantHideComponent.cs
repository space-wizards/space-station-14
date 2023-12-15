using Content.Server.Plants.Systems;
using Content.Server.Storage.Components;
using Robust.Shared.Audio;

namespace Content.Server.Plants.Components
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
