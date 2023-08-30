using System.Numerics;
using Content.Shared.Pointing.Components;

namespace Content.Client.Pointing.Components;
[RegisterComponent]
public sealed partial class PointingArrowComponent : SharedPointingArrowComponent
{
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public float AnimationTime = 0.5f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public Vector2 Offset = new(0, 0.25f);

    public readonly string AnimationKey = "pointingarrow";
}
