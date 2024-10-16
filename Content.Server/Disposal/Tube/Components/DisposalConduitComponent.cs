using Content.Server.Disposal.Unit.EntitySystems;

namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent]
[Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
public sealed partial class DisposalConduitComponent : Component
{
    /// <summary> 
    /// Array of angles that entities can exit the conduit from
    /// </summary>
    /// <remarks>
    /// Entities will preferentially exit the conduit at the first angle listed
    /// </remarks>
    [DataField]
    public Angle[] Angles = { 0 };

    /// <summary> 
    /// The smallest angle that entities can turn while traveling through the conduit
    /// </summary>
    [DataField]
    public Angle MinDeltaAngle = 0;

}
