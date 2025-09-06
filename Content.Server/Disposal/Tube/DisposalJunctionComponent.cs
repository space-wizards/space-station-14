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
    public List<Angle> Degrees = [];

    /// <summary>
    /// If true, items entering from the outlet will try to continue straight, falling back
    /// to the behavior described below if that's not possible.
    ///
    /// There are different behaviors if false for routers versus junctions for backwards
    /// compatability reasons. If false, routers will treat items that enter from its outlet
    /// as if it was the inlet, while junctions will choose any random non-outlet direction.
    /// </summary>
    [DataField]
    public bool BackwardsAllowed;
}
