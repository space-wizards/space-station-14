using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FootstepModifierComponent : Component
    {
        [DataField("footstepSoundCollection", required: true)]
        public SoundSpecifier Sound = default!;
    }
}
