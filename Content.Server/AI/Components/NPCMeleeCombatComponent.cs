namespace Content.Server.AI.Components;

/// <summary>
/// Added to NPCs whenever they're in melee combat so they can be handled by the dedicated system.
/// </summary>
[RegisterComponent]
public sealed class NPCMeleeCombatComponent : Component
{
    [ViewVariables]
    public EntityUid Target;
}
