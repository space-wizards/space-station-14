using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chasm;

/// <summary>
///     Added to entities which have started falling into a chasm.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChasmFallingComponent : Component
{
    /// <summary>
    ///     Time it should take for the falling animation (scaling down) to complete.
    /// </summary>
    [DataField("animationTime")]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     Time it should take in seconds for the entity to actually delete
    /// </summary>
    [DataField("deletionTime")]
    public TimeSpan DeletionTime = TimeSpan.FromSeconds(1.8f);

    [DataField("nextDeletionTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextDeletionTime = TimeSpan.Zero;

    /// <summary>
    ///     Original scale of the object so it can be restored if the component is removed in the middle of the animation
    /// </summary>
    public Vector2 OriginalScale = Vector2.Zero;

    /// <summary>
    ///     Scale that the animation should bring entities to.
    /// </summary>
    public Vector2 AnimationScale = new Vector2(0.01f, 0.01f);
}
