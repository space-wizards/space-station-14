namespace Content.Server.ParticleAccelerator.Components;

[RegisterComponent]
public sealed class ParticleAcceleratorPartComponent : Component
{
    [ViewVariables]
    public EntityUid? Master;
}
