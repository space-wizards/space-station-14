namespace Content.Shared.Throwing;

/// <summary>
/// Modifies the speed value of a TryThrow.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class ThrowSpeedModifierComponent : Component
{
    /// <summary>
    /// Modifies the speed at which the entity is thrown by adding its value to it.
    /// It will not take effect if the result would be or go below 0.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FlatModifier;

    /// <summary>
    /// Modifies the speed at which the entity is thrown by multiplying its value.
    /// It will not take effect if the result would be or go below 0.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 1;
}
