using Content.Server.Plants.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Plants.Components
{
    /// <summary>
    ///     Interaction wrapper for <see cref="SecretStashComponent"/>.
    ///     Gently rustle after each interaction with plant.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(PottedPlantHideSystem))]
    public class PottedPlantHideComponent : Component
    {
        [DataField("rustleSound")]
        public SoundSpecifier RustleSound = new SoundPathSpecifier("/Audio/Effects/plant_rustle.ogg");
    }
}
