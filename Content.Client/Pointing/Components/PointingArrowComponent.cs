using Content.Shared.Pointing.Components;

namespace Content.Client.Pointing.Components;
[RegisterComponent]
public sealed class PointingArrowComponent : SharedPointingArrowComponent
{
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public readonly float AnimationTime = 0.5f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public readonly Vector2 Offset = (0, 0.25f);

    public readonly string AnimationKey = "pointingarrow";
}
