using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Wall;

/// <summary>
/// This component parents an entity to a wall on MapInit if it can find one.
/// We do not change the actual parent of the transform, since this causes issues with entity anchoring.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ParentToWallSystem))]
public sealed partial class ParentToWallComponent : Component
{
    /// <summary>
    /// The offset (local to the entity) the tile lookup should be in.
    /// To offset forward (i.e. the wall should be to the south when facing south), this should be -Vector2.UnitY.
    /// </summary>
    [DataField]
    public Vector2 Offset;

    /// <summary>
    /// The parent of this entity - the wall it is attached to.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? Parent;

    /// <summary>
    /// Is this entity important enough to prevent deconstruction of the wall?
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool BlockDeconstruction = true;

    /// <summary>
    /// Should this entity be anchored when the wall is?
    /// Should remain false for items like directional signs that may want an offset.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Anchor = true;

    /// <summary>
    /// The last anchored state of the entity.
    /// If the entity is unexpectedly (un)anchored we will unparent it from the wall.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [DataField]
    [AutoNetworkedField]
    public bool Anchored;
}
