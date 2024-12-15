using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is currently being conveyed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConveyedComponent : Component
{
    // Track last position and see if the conveyed item is no longer moving (e.g. into a wall), in which case stop conveying.
    // At the moment we don't have shapecasts so can't check if we'd move it into a valid shape or not so
    // TODO: When shapecasts in check if movement is valid and if not then don't move.

    [DataField]
    public Vector2? LastPosition;

    [DataField]
    public float StopTimer;

    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Colliding = new();
}
