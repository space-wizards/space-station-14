using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.EntitySystems;

namespace Content.Server.Body.Network
{
    /// <summary>
    ///     Represents a "network" such as a bloodstream or electrical power that is coordinated throughout an entire
    ///     <see cref="BodyManagerComponent"/>.
    /// </summary>
    public abstract class BodyNetwork
    {
        public abstract void OnCreate();

        public abstract void OnDelete();

        /// <summary>
        ///     Called every frame by <see cref="BodySystem"/>.
        /// </summary>
        public abstract void OnTick(float frameTime);
    }
}
