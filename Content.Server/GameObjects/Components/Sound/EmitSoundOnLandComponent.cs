using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Simple sound emitter that emits sound on land
    /// </summary>
    [RegisterComponent]
    public class EmitSoundOnLandComponent : BaseEmitSoundComponent, ILand
    {
        /// <inheritdoc />
        ///
        public override string Name => "EmitSoundOnLand";

        void ILand.Land(LandEventArgs eventArgs)
        {
            PlaySoundBasedOnMode();
        }
    }
}
