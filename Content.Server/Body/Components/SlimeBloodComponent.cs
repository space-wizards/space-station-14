namespace Content.Server.Body.Components;

/// <summary>
/// Used by the SlimeSystem to respond to BloodColorOverrideEvents.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeBloodComponent : Component
{
    /// <summary>
    /// If set, the SlimeBloodSystem will set the slime color to this.
    /// Useful for slime creatures. If null, the color is derived from
    /// HumanoidAppearanceComponent.SkinColor.
    /// </summary>
    [DataField]
    public Color? ManualOverrideColor = null;
}
