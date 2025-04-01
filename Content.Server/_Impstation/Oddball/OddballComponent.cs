using Content.Shared.FixedPoint;

namespace Content.Server._Impstation.Oddball;

[RegisterComponent]
public sealed partial class OddballComponent : Component
{
    /// <summary>
    /// Whether the oddball is picked up and should be awarding points.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// The entity currently holding the oddball.
    /// </summary>
    [DataField]
    public EntityUid? Holder;

    /// <summary>
    /// The next time oddball points will be awarded.
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval between each point award.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The amount of points awarded every update interval.
    /// </summary>
    [DataField]
    public FixedPoint2 PointValue = 1;
}
