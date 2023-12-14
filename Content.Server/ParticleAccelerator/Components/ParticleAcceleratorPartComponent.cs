namespace Content.Server.ParticleAccelerator.Components;

[RegisterComponent]
public sealed partial class ParticleAcceleratorPartComponent : Component
{
    [ViewVariables]
    public EntityUid? Master;
}
