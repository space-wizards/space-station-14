using Content.Server.Sound.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Interaction.Components
{
    /// <summary>
    /// Simple sound emitter that emits sound on UseInHand
    /// </summary>
    [RegisterComponent]
    public sealed class EmitSoundOnUseComponent : BaseEmitSoundComponent
    {
        /// <summary>
        ///     Whether or not to mark an interaction as handled after playing the sound. Useful if this component is
        ///     used to play sound for some other component with on-use functionality
        /// </summary>
        [DataField("handle")]
        public bool Handle = true;
    }
}
