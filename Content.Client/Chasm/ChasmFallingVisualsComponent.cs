using System.Numerics;

namespace Content.Client.Chasm;

[RegisterComponent]
public sealed partial class ChasmFallingVisualsComponent : Component
{
    [ViewVariables]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     Original scale of the object so it can be restored if the component is removed in the middle of the animation
    /// </summary>
    [ViewVariables]
    public Vector2? OriginalScale;

    /// <summary>
    ///     Scale that the animation should bring entities to.
    /// </summary>
    [ViewVariables]
    public Vector2 AnimationScale = new Vector2(0.01f, 0.01f);
}
