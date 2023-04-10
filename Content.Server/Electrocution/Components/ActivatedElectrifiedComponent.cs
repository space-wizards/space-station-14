namespace Content.Server.Electrocution;

/// <summary>
/// Updates every frame for short duration to check if electrifed entity is powered when activated, e.g to play animation
/// </summary>
[RegisterComponent]
public sealed class ActivatedElectrifiedComponent : Component
{
    /// <summary>
    /// How long electrified entity will remain active
    /// </summary>
    [DataField("lifetime")]
    public float Lifetime = 5f;
}
