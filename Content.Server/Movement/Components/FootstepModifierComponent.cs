using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Movement.Components
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    [RegisterComponent]
    public class FootstepModifierComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "FootstepModifier";

        [DataField("footstepSoundCollection")]
        public SoundSpecifier _soundCollection = default!;

        public void PlayFootstep()
        {
            if (_soundCollection.TryGetSound(out var footstepSound))
                SoundSystem.Play(Filter.Pvs(Owner), footstepSound, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2f));
        }
    }
}
