using Robust.Shared.GameObjects;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Simple sound emitter that emits sound on ActivateInWorld
    /// </summary>
    [RegisterComponent]
    public sealed class EmitSoundOnActivateComponent : BaseEmitSoundComponent
    {
        /// <summary>
        ///     Whether or not to mark an interaction as handled after playing the sound. Useful if this component is
        ///     used to play sound for some other component with activation functionality
        /// </summary>
        [DataField("handle")]
        public bool Handle = true;
    }
}
