using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    [RegisterComponent]
    public class FootstepModifierComponent : BaseEmitSoundComponent
    {
        /// <inheritdoc />
        public override string Name => "FootstepModifier";
    }
}
