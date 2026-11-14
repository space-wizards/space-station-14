using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

/// <summary>
/// A projectile with this component will not deal <see cref="ProjectileComponent.Damage"/>.
/// Instead, it will do damage based on the target if
/// said target passes the whitelist and blacklist of the options
/// </summary>
/// <remarks>
/// If the target doesn't match any option, the projectile will deal <see cref="ProjectileComponent.Damage"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ComplexProjectileDamageComponent : Component
{
    /// <summary>
    /// Array of options of damage for specific entities.
    /// </summary>
    /// <remarks>
    /// Only one option will choosen, if a target fits 2 options,
    /// it will receive damage only from the first option that matches.
    /// </remarks>
    [DataField]
    public DamageOption[] DamageOptions = [];
}

/// <summary>
/// Specifies a amount of damage to deal to entities that fit the whitelist and are not blacklisted.
/// </summary>
[DataDefinition]
public partial struct DamageOption
{
    /// <summary>
    /// The damage to deal to certain entities.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage;

    /// <summary>
    /// The whitelist of entities to deal the damage to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The blacklist of entities to not deal the damage to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
