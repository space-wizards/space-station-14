namespace Content.Server._FTL.AutomatedCombat;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class ActiveAutomatedCombatComponent : Component
{
    [ViewVariables] public float TimeSinceLastAttack = 5f;
}
