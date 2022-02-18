using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IAtmosphereComponent))]
    public sealed class SpaceAtmosphereComponent : Component, IAtmosphereComponent
    {
        public bool Simulated => false;
    }
}
