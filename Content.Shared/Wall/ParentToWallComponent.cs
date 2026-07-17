using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Wall;

/// <summary>
/// This component parents an entity to a wall on MapInit if it can find one.
/// We do not change the actual parent of the transform, since this causes issues with entity anchoring.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParentToWallComponent : Component
{
    /// <summary>
    /// The offset (local to the entity) the tile lookup should be in.
    /// To offset forward (i.e. the wall should be to the south when facing south), this should be -Vector2i.UnitY.
    /// </summary>
    [DataField]
    public Vector2 Offset;

    /// <summary>
    /// The parent of this entity - the wall it is attached to.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? Parent;
}
