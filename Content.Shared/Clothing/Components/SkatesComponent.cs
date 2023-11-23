using Robust.Shared.GameStates;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared.Clothing;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SkatesSystem))]
public sealed partial class SkatesComponent : Component
{
    /// <summary>
    /// the levels of friction the wearer is subected to, higher the number the more friction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Friction = 5;

    /// <summary>
    /// Determines the turning ability of the wearer, Higher the number the less control of their turning ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float? FrictionNoInput = 5f;

    /// <summary>
    /// Sets the speed in which the wearer accelerates to full speed, higher the number the quicker the acceleration.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Acceleration = 10f;

    /// <summary>
    /// The minimum speed the wearer needs to be traveling to take damage from collision.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinimumSpeed = 4f;

    /// <summary>
    /// The length of time the wearer is stunned for on collision.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float StunSeconds = 1f;

    /// <summary>
    /// The time duration before another collision can take place.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float DamageCooldown = 2f;

    /// <summary>
    /// The damage per increment of speed on collision.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float SpeedDamage = 0.5f;

    /// <summary>
    /// Defaults for MinimumSpeed, StunSeconds, DamageCooldown and SpeedDamage.
    /// </summary>
    [ViewVariables]
    public float DefaultMinimumSpeed = 20f;

    [ViewVariables]
    public float DefaultStunSeconds = 1f;

    [ViewVariables]
    public float DefaultDamageCooldown = 2f;

    [ViewVariables]
    public float DefaultSpeedDamage = 0.5f;
}
