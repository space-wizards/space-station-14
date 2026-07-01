using Content.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Shared.Corporeal.Components;

/// <summary>
/// Makes the target solid and visible.
/// Warning: fragile if multiple instances affect the same target at once.
/// </summary>
[RegisterComponent]
public sealed partial class CorporealStatusEffectComponent : Component
{
    /// <summary>
    /// Sprite used for the corporeal overlay.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite;

    /// <summary>
    /// The collision mask applied while the target is corporeal.
    /// </summary>
    [DataField]
    public int CollisionMask = (int)(CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable);

    /// <summary>
    /// The collision layer applied while the target is corporeal.
    /// </summary>
    [DataField]
    public int CollisionLayer = (int)CollisionGroup.SmallMobLayer;

    /// <summary>
    /// The collision mask restored when the corporeal effect ends.
    /// </summary>
    [DataField]
    public int RemovedCollisionMask = (int)CollisionGroup.GhostImpassable;

    /// <summary>
    /// The collision layer restored when the corporeal effect ends.
    /// </summary>
    [DataField]
    public int RemovedCollisionLayer;
}
