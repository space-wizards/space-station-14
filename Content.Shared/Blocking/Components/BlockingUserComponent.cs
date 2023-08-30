using Robust.Shared.Physics;

namespace Content.Shared.Blocking;

/// <summary>
/// This component gets dynamically added to an Entity via the <see cref="BlockingSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class BlockingUserComponent : Component
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
}
