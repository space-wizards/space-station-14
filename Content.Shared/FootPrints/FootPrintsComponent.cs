using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Shared.FootPrints;
[RegisterComponent]
public sealed class FootPrintsComponent : Component
{
    [DataField("leftPrint")]
    public SpriteSpecifier LeftPrint { get; } = SpriteSpecifier.Invalid;

    [DataField("rightPrint")]
    public SpriteSpecifier RightPrint { get; } = SpriteSpecifier.Invalid;

    public Vector2 OffsetCenter = new Vector2(-0.5f, -1f);
    public Vector2 OffsetPrint = new Vector2(0.1f, 0f);

    [ViewVariables(VVAccess.ReadWrite), DataField("time")]
    public TimeSpan Time = TimeSpan.FromSeconds(0.3f);
    public TimeSpan Timer = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color PrintsColor = Color.FromHex("#FF0000FF");
    [ViewVariables(VVAccess.ReadWrite), DataField("paintQuality")]
    public float PaintQuality = 0f;

    [ViewVariables(VVAccess.ReadWrite), DataField("myAngle")]
    public float Angle = 0f;

    public bool RightStep = true;
}
