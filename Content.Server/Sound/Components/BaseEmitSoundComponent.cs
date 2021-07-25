using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Base sound emitter which defines most of the data fields.
    /// Accepts both single sounds and sound collections.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier Sound { get; set; } = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("variation")]
        public float PitchVariation { get; set; } = 0.0f;
    }
}
