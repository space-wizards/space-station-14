using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.PA
{
    public abstract class ParticleAcceleratorPartComponent : Component
    {
        [ViewVariables] public ParticleAccelerator ParticleAccelerator { get; set; } = new ParticleAccelerator();
    }
}
