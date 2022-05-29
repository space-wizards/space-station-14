using Content.Shared.Sound;
using Robust.Shared.Audio;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Base sound emitter which defines most of the data fields.
    /// Accepts both single sounds and sound collections.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound", required: true)]
        public SoundSpecifier Sound { get; set; } = default!;

        [DataField("audioParams")]
        public AudioParams AudioParams = AudioParams.Default.WithVolume(-2f);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("variation")]
        public float PitchVariation { get; set; } = 0.0f;
    }
}
