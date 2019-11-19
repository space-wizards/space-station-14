using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Mobs will only make footstep sounds if they have this component.
    /// </summary>
    [RegisterComponent]
    public class FootstepSoundComponent : Component
    {
        public override string Name => "FootstepSound";
    }
}
