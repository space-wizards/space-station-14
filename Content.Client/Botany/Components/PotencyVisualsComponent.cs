namespace Content.Client.Botany.Components;

[RegisterComponent]
public sealed class PotencyVisualsComponent : Component
{
    [DataField("minimumScale")]
    public float MinimumScale = 1f;

    [DataField("maximumScale")]
    public float MaximumScale = 2f;
}
