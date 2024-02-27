namespace Content.Client.Clickable;

/// <summary>
/// Makes it possible to click the associated entity.
/// </summary>
[RegisterComponent]
public sealed partial class ClickableComponent : Component
{
    /// <summary>
    /// A set of AABBs used as an approximate check for whether a click could hit this entity.
    /// </summary>
    [DataField("bounds")]
    public DirBoundData? Bounds;

    /// <summary>
    /// A set of AABBs associated with the cardinal directions used for approximate click intersection calculations.
    /// </summary>
    [DataDefinition]
    public sealed partial class DirBoundData
    {
        [DataField("all")] public Box2 All;
        [DataField("north")] public Box2 North;
        [DataField("south")] public Box2 South;
        [DataField("east")] public Box2 East;
        [DataField("west")] public Box2 West;
    }
}
