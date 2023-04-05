using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed class SmokeComponent : Component
{
    public const string SolutionName = "solutionArea";

    [DataField("nextReact", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextReact = TimeSpan.Zero;

    [DataField("spreadAmount")]
    public int SpreadAmount = 0;

    /// <summary>
    ///     Have we reacted with our tile yet?
    /// </summary>
    [DataField("reactedTile")]
    public bool ReactedTile = false;

    /// <summary>
    /// Solution threshold to overflow to a neighbouring tile.
    /// </summary>
    [DataField("overflow")]
    public FixedPoint2 OverflowThreshold = FixedPoint2.New(20);
}
