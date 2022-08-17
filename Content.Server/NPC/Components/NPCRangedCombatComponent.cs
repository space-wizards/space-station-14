namespace Content.Server.NPC.Components;

/// <summary>
/// Added to an NPC doing ranged combat.
/// </summary>
[RegisterComponent]
public sealed class NPCRangedCombatComponent : Component
{
    // TODO: Have some abstract component they inherit from.
    [ViewVariables] public EntityUid Weapon;

    [ViewVariables]
    public EntityUid Target;

    [ViewVariables]
    public CombatStatus Status = CombatStatus.TargetNormal;

    /// <summary>
    /// In radians. If null it will instantly turn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float? RotationSpeed;
}
