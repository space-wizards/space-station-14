namespace Content.Shared._MACRO.Atmos.Components;

/// <summary>
/// This is used for modifying fire stacks.
/// </summary>
[RegisterComponent]
public sealed partial class FireStackModifierComponent : Component
{
    /// <summary>
    /// The modifier to multiply the fire stacks by.
    /// </summary>
    [DataField]
    public float Modifier { get; set; } = 1f;
}
