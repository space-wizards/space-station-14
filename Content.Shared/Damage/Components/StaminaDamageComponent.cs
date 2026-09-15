namespace Content.Shared.Damage.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public abstract partial class StaminaDamageComponent : Component
{
    [DataField]
    public float Damage = 55f;
}
