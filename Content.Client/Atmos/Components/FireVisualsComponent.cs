namespace Content.Client.Atmos.Components;

/// <summary>
/// Sets which sprite RSI is used for displaying the fire visuals and what state to use based on the fire stacks
/// accumulated.
/// </summary>
[RegisterComponent]
public sealed partial class FireVisualsComponent : Component
{
    [DataField("fireStackAlternateState")]
    public int FireStackAlternateState = 3;

    [DataField("normalState")]
    public string? NormalState;

    [DataField("alternateState")]
    public string? AlternateState;

    [DataField("sprite")]
    public string? Sprite;

    [DataField("lightEnergyPerStack")]
    public float LightEnergyPerStack = 0.5f;

    [DataField("lightRadiusPerStack")]
    public float LightRadiusPerStack = 0.3f;

    [DataField("maxLightEnergy")]
    public float MaxLightEnergy = 10f;

    [DataField("maxLightRadius")]
    public float MaxLightRadius = 4f;

    [DataField("lightColor")]
    public Color LightColor = Color.Orange;

    /// <summary>
    ///     Client side point-light entity. We use this instead of directly adding a light to
    ///     the burning entity as entities don't support having multiple point-lights.
    /// </summary>
    public EntityUid? LightEntity;
}
