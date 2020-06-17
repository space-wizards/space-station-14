using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Strap
{
    public enum StrapPosition
    {
        /// <summary>
        /// (Default) Mob is standing up
        /// </summary>
        Standing = 0,

        /// <summary>
        /// Mob is laying down
        /// </summary>
        Down
    }

    public abstract class SharedStrapComponent : Component
    {
        public sealed override string Name => "Strap";

        public virtual StrapPosition Position { get; set; }
    }
}
