using System.Numerics;
using Content.Shared.Pointing.Components;

namespace Content.Client.Pointing.Components;
[RegisterComponent]
public sealed partial class PointingArrowComponent : SharedPointingArrowComponent
{
    /// <summary>
    /// How far the arrow moves up and down during the floating phase.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public Vector2 Offset = new(0, 0.25f);

    public readonly string AnimationKey = "pointingarrow";
}
