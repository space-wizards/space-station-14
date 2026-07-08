namespace Content.Client.Botany.Components;

[RegisterComponent]
public sealed partial class PotencyVisualsComponent : Component
{
    [DataField]
    public float MinimumScale = 1f;

    [DataField]
    public float MaximumScale = 2f;
}
