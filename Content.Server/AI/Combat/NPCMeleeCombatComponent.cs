namespace Content.Server.AI.Combat;

/// <summary>
/// Added to NPCs whenever they're in melee combat so they can be handled by the dedicated system.
/// </summary>
[RegisterComponent]
public sealed class NPCMeleeCombatComponent : Component
{
    /// <summary>
    /// Weapon we're using to attack the target. Can also be ourselves.
    /// </summary>
    [ViewVariables] public EntityUid Weapon;

    [ViewVariables]
    public EntityUid Target;

    [ViewVariables]
    public CombatStatus Status = CombatStatus.TargetNormal;
}

public enum CombatStatus : byte
{
    /// <summary>
    /// Set if we can't reach the target for whatever reason.
    /// </summary>
    TargetUnreachable,

    /// <summary>
    /// Set if the target is valid but still alive.
    /// </summary>
    TargetNormal,

    /// <summary>
    /// Set if the target is crit.
    /// </summary>
    TargetCrit,

    /// <summary>
    /// Set if the target is dead.
    /// </summary>
    TargetDead,

    /// <summary>
    /// Set if the weapon we were assigned is no longer valid.
    /// </summary>
    NoWeapon,
}
