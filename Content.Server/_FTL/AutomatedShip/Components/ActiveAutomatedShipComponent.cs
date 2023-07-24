namespace Content.Server._FTL.AutomatedShip.Components;

/// <summary>
/// This is used for tracking things in active combat
/// </summary>
[RegisterComponent]
public sealed class ActiveAutomatedShipComponent : Component
{
    [ViewVariables] public float TimeSinceLastAttack = 5f;
}
