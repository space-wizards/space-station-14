using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Simple sound emitter that emits sound on ThrowEvent
    /// </summary>
    [RegisterComponent]
    public class EmitSoundOnThrowComponent : BaseEmitSoundComponent
    {
        /// <inheritdoc />
        ///
        public override string Name => "EmitSoundOnThrow";
    }
}
