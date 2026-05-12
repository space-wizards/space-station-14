namespace Content.Server._EE.Flash.Components;

/// <summary>
/// Makes the entity take eye damage and longer flashes when flashed.
/// Used for races with sensitive eyes (e.g. Felinid).
/// </summary>
[RegisterComponent]
public sealed partial class EyeDamageOnFlashingComponent : Component
{
    /// <summary>
    /// Multiplier applied to the flash duration against this entity.
    /// </summary>
    [DataField]
    public float FlashDurationMultiplier = 1.5f;

    /// <summary>
    /// Chance (0..1) to receive permanent eye damage on each successful flash.
    /// </summary>
    [DataField]
    public float EyeDamageChance = 0.3f;

    /// <summary>
    /// Amount of permanent eye damage applied if <see cref="EyeDamageChance"/> rolls succeed.
    /// </summary>
    [DataField]
    public int EyeDamage = 1;
}
