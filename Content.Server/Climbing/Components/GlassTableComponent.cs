using Content.Shared.Damage;

namespace Content.Server.Climbing.Components;

/// <summary>
///     Glass tables shatter and stun you when climbed on.
///     This is a really entity-specific behavior, so opted to make it
///     not very generalized with regards to naming.
/// </summary>
[RegisterComponent, Access(typeof(ClimbSystem))]
public sealed partial class GlassTableComponent : Component
{
    /// <summary>
    ///     How much damage should be given to the climber?
    /// </summary>
    [DataField("climberDamage")]
    public DamageSpecifier ClimberDamage = default!;

    /// <summary>
    ///     How much damage should be given to the table when climbed on?
    /// </summary>
    [DataField("tableDamage")]
    public DamageSpecifier TableDamage = default!;

    /// <summary>
    ///     How much mass should be needed to break the table?
    /// </summary>
    [DataField("tableMassLimit")]
    public float MassLimit;

    /// <summary>
    ///     How long should someone who climbs on this table be stunned for?
    /// </summary>
    public float StunTime = 2.0f;
}
