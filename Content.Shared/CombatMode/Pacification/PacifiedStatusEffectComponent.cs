using Robust.Shared.GameStates;

namespace Content.Shared.CombatMode.Pacification;

/// <summary>
/// Status effect that disallows harming living things and restricts aggressive actions.
///
/// There is a caveat with pacifism. It's not intended to be wholly encompassing: there are ways of harming people
/// while pacified--plenty of them, even! The goal is to restrict the obvious ones to make gameplay more interesting
/// while not overly limiting.
///
/// If you want full-pacifism (no combat mode at all), you can simply set <see cref="DisallowAllCombat"/> before adding.
///
/// Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(PacificationSystem))]
public sealed partial class PacifiedStatusEffectComponent : Component
{
    /// <summary>
    /// If true, this will prevent you from disarming opponents in combat.
    /// </summary>
    [DataField]
    public bool DisallowDisarm;

    /// <summary>
    /// If true, this will disable combat entirely instead of only disallowing attacking living creatures and harmful things.
    /// </summary>
    [DataField]
    public bool DisallowAllCombat;
}
