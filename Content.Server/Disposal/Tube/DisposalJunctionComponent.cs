namespace Content.Server.Disposal.Tube;

[RegisterComponent]
[Access(typeof(DisposalTubeSystem))]
[Virtual]
public partial class DisposalJunctionComponent : Component
{
    /// <summary>
    /// The angles to connect to.
    /// </summary>
    [DataField]
    public List<Angle> Degrees = new();

    /// <summary>
    /// Whether transported entities should try to follow
    /// the straightest path through a junction
    /// </summary>
    [DataField]
    public bool FollowStraightestPath = false;
}
