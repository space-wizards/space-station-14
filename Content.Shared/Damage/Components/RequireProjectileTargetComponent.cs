namespace Content.Shared.Damage.Components;

/// <summary>
/// Prevent the object from getting hit by projetiles unless you target the object.
/// </summary>
[RegisterComponent]
public sealed partial class RequireProjectileTargetComponent : Component
{
    [DataField]
    public bool Active = true;
}
