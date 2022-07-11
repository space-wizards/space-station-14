using Robust.Shared.Audio;

namespace Content.Shared.Movement.Components
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    [RegisterComponent]
    public sealed class FootstepModifierComponent : Component
    {
        [DataField("footstepSoundCollection", required: true)]
        public SoundSpecifier SoundCollection = default!;

        [DataField("variation")]
        public float Variation = default;
    }
}
