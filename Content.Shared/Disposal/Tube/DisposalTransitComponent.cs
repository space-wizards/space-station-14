using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Basic component for disposal pipes - contains a list of the potential
/// directions disposal holders passing through it can be routed.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDisposalTubeSystem))]
[Virtual]
public partial class DisposalTransitComponent : Component
{
    /// <summary>
    /// Array of angles that entities can exit the disposal tube from.
    /// South is 0, west is 90, north is 180, and east is -90.
    /// </summary>
    /// <remarks>
    /// The direction that entities will exit preferentially follows the order the list (from most to least).
    /// A direction will be skipped if it is the opposite to the direction the entity entered,
    /// or if the angular difference between the entry and potential exit is less than <see cref="MinDeltaAngle"/>.
    /// </remarks>
    [DataField]
    public Angle[] Degrees = { 0 };

    /// <summary>
    /// The smallest angle that entities can turn while traveling through the conduit.
    /// </summary>
    [DataField]
    public Angle MinDeltaAngle = 0;
}
