namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// Used for hidden entities that gimbal guns point at.
/// </summary>
[RegisterComponent]
public sealed class GimbalGunTargetComponent : Component
{
    public SharedTurretConsoleComponent? Console;
}
