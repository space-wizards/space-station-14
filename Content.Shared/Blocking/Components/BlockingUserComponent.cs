using Robust.Shared.Physics;

namespace Content.Shared.Blocking;

/// <summary>
/// This component gets dynamically added to an Entity via the <see cref="BlockingSystem"/>
/// </summary>
[RegisterComponent]
public sealed class BlockingUserComponent : Component
{
    /// <summary>
    /// The entity that's being used to block
    /// </summary>
    [DataField("blockingItem")]
    public EntityUid? BlockingItem;

    /// <summary>
    /// Stores the entities original bodytype
    /// Used so that it can be put back to what it was after anchoring
    /// </summary>
    [DataField("originalBodyType")]
    public BodyType OriginalBodyType;

    /// <summary>
    /// Stores the entities original walk speed
    /// Used so that it can be put back to what it was before raising the shield
    /// </summary>
    [DataField("originalWalkSpeed")]
    public float OriginalWalkSpeed;

    /// <summary>
    /// Stores the entities original sprint speed
    /// Used so that it can be put back to what it was before raising the shield
    /// </summary>
    [DataField("originalSprintSpeed")]
    public float OriginalSprintSpeed;

    /// <summary>
    /// Stores the entities original acceleration
    /// Used so that it can be put back to what it was before raising the shield
    /// </summary>
    [DataField("originalAcceleration")]
    public float OriginalAcceleration;
}
