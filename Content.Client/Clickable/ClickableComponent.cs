namespace Content.Client.Clickable;

[RegisterComponent]
public sealed partial class ClickableComponent : Component
{
    [DataField] public DirBoundData? Bounds;

    [DataDefinition]
    public sealed partial class DirBoundData
    {
        [DataField] public Box2 All;
        [DataField] public Box2 North;
        [DataField] public Box2 South;
        [DataField] public Box2 East;
        [DataField] public Box2 West;
    }
}
