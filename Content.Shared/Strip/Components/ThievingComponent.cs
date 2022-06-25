namespace Content.Shared.Strip.Components;

[RegisterComponent]
public sealed class ThievingComponent : Component
{
    [ViewVariables]
    [DataField("stealTime")]
    public float StealTime = 0.5f;
}
