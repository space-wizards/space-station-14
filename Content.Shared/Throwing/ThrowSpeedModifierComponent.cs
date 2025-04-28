namespace Content.Shared.Throwing;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class ThrowSpeedModifierComponent : Component
{
    /// <summary>
    /// Modifies the speed at which the entity is thrown by hand by adding its value to it.
    /// It will not take effect if the result would be or go below 0.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Modifier;
}
