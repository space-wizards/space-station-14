using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Components
{
    public interface IAtmosphereComponent : IComponent
    {
        /// <summary>
        ///     Whether this atmosphere is simulated or not.
        /// </summary>
        bool Simulated { get; }
    }
}
