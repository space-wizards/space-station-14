namespace Content.Server.Disposal.Tube.Components;

// TODO: Different types of tubes eject in random direction with no exit point
/// <summary>
/// Makes contents go along the pipe with no special routing.
/// Anything with 2 holes will probably use this.
/// </summary>
[RegisterComponent, Access(typeof(DisposalTubeSystem))]
public sealed partial class DisposalTransitComponent : Component
{
}
