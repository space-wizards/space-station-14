namespace Content.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class MadnessMaskComponent : Component
{
    public float UpdateAccumulator = 0f;
    [DataField] public float UpdateTimer = 1f;
}
