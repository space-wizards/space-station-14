namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent, Access(typeof(DisposalTubeSystem))]
[Virtual]
public partial class DisposalJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField]
    public List<Angle> Degrees = new();
}
