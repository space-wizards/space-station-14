using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Turns an entity into a disposal tube.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DisposalTubeSystem))]
public sealed partial class DisposalTubeComponent : Component
{
    /// <summary>
    /// Array of directions that entities can potentially exit the disposal tube.
    /// </summary>
    /// <remarks>
    /// The direction that entities exit preferentially follows the order the list
    /// (from most to least). A direction will be skipped if it is the opposite to
    /// the direction the entity entered the tube, or the tube has modifying components.
    /// </remarks>
    [DataField]
    public Direction[] Exits = { Direction.South };
}

/// <summary>
/// Visual state for tagging items detectable by T-ray scanners
/// </summary>
[Serializable, NetSerializable]
public enum DisposalTubeVisuals
{
    VisualState
}
