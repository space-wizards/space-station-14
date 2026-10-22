using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.CombatMode;

[RegisterComponent, NetworkedComponent]
public sealed partial class CombatModeSprintComponent : Component
{
    [DataField]
    public float SprintCoefficient = 1.5f;

    [DataField]
    public bool DoImpactDamage;

    [DataField]
    public LocId? BeginCombatMessage = null;
    [DataField]
    public LocId? EndCombatMessage = null;

    /// <summary>
    /// Settings for impact damage, if applicable.
    /// </summary>
    [DataField]
    public float MinimumSpeed = 3f;
    [DataField]
    public float StunSeconds = 3f;
    [DataField]
    public float DamageCooldown = 2f;
    [DataField]
    public float SpeedDamage = 1f;


    /// <summary>
    /// Defaults to reset to for impact damage. I would personally rather derive these in the code, but this is how SkatesComponent does it.
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
