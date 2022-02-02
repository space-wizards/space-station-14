using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IAtmosphereComponent))]
    public class SpaceAtmosphereComponent : Component, IAtmosphereComponent
    {
        public bool Simulated => false;
    }
}
