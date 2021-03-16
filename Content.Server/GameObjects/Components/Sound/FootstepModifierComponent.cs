using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    [RegisterComponent]
    public class FootstepModifierComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _footstepRandom = default!;

        /// <inheritdoc />
        public override string Name => "FootstepModifier";

        [DataField("footstepSoundCollection")]
        public string? _soundCollectionName;

        public void PlayFootstep()
        {
            if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
                var file = _footstepRandom.Pick(soundCollection.PickFiles);
                EntitySystem.Get<AudioSystem>().PlayFromEntity(file, Owner, AudioParams.Default.WithVolume(-2f));
            }
        }
    }
}
