using Content.Server.Sound.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Interaction.Components
{
    /// <summary>
    /// Simple sound emitter that emits sound on UseInHand
    /// </summary>
    [RegisterComponent]
    public class EmitSoundOnUseComponent : BaseEmitSoundComponent
    {
        /// <inheritdoc />
        public override string Name => "EmitSoundOnUse";
    }
}
