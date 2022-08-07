using Content.Shared.Beam.Components;

namespace Content.Server.Beam.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedBeamComponent))]
public sealed class BeamComponent : SharedBeamComponent
{

}
