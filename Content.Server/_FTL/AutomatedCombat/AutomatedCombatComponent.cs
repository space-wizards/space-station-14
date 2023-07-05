namespace Content.Server._FTL.AutomatedCombat;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class AutomatedCombatComponent : Component
{
    /// <summary>
    /// How long does it take to fire a weapon?
    /// </summary>
    [DataField("attackRepetition")] [ViewVariables(VVAccess.ReadWrite)]
    public float AttackRepetition = 15f;
}
