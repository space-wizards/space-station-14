namespace Content.Shared.Bible;

/// <summary>
/// This is used to listen to incoming events from the AppearanceSystem
/// </summary>
[RegisterComponent]
public sealed partial class BlessedVisualsComponent : Component
{
    /// <summary>
    /// The brightness of the holy light
    /// </summary>
    [DataField]
    public float LightEnergy = 20f;

    /// <summary>
    /// The distance the holy light will cast
    /// </summary>
    [DataField]
    public float LightRadius = 1.5f;

    /// <summary>
    /// The color of the holy light
    /// </summary>
    [DataField]
    public Color LightColor = Color.Linen;

    /// <summary>
    ///     Client side point-light entity. We use this instead of directly adding a light to
    ///     the blessed entity as entities don't support having multiple point-lights.
    /// </summary>
    public EntityUid? LightEntity;
}
