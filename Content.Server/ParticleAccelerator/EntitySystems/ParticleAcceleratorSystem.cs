namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeControlBoxSystem();
        InitializePartSystem();
        InitializePowerBoxSystem();
    }
}
