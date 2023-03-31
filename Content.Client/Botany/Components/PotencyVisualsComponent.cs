namespace Content.Client.Botany.Components;

[RegisterComponent]
public sealed class PotencyVisualsComponent : Component
{
    [DataField("minimumScale")]
    public float MinimumScale = 0.5f;

    [DataField("maximumScale")]
    public float MaximumScale = 1.5f;
}
