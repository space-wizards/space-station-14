using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Movement.Components
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
                SoundSystem.Play(Filter.Pvs(Owner), file, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2f));
            }
        }
    }
}
