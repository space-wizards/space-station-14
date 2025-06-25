namespace Content.Shared.Throwing;

/// <summary>
/// Modifies the speed value of a TryThrow.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class ThrowSpeedModifierComponent : Component
{
    /// <summary>
    /// Modifies the speed at which the entity is thrown by adding its value to it.
    /// It will default to 0 if the result would be lower than that.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FlatModifier;

    /// <summary>
    /// Modifies the speed at which the entity is thrown by multiplying its value.
    /// It will default to 0 if the result would be lower than that.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 1;
}
