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

        [DataField("footstepSoundCollection", required: true)]
        public SoundSpecifier SoundCollection = default!;

        public void PlayFootstep()
        {
            SoundSystem.Play(Filter.Pvs(Owner), SoundCollection.GetSound(), Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2f));
        }
    }
}
