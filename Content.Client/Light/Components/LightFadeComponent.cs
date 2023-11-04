namespace Content.Client.Light.Components;

/// <summary>
/// Fades out the <see cref="SharedPointLightComponent"/> attached to this entity.
/// </summary>
[RegisterComponent]
public sealed partial class LightFadeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("duration")]
    public float Duration = 0.5f;
}
