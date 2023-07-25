namespace Content.Server._FTL.AutomatedShip.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class AutomatedShipComponent : Component
{
    /// <summary>
    /// The two states of an AI. It can either cruise, AKA idle or go into combat.
    /// </summary>
    public enum AiStates
    {
        Cruising,
        Fighting
    }

    /// <summary>
    /// How long does it take to fire a weapon?
    /// </summary>
    [DataField("attackRepetition"), ViewVariables(VVAccess.ReadWrite)]
    public float AttackRepetition = 15f;

    /// <summary>
    /// The next point the ship would like to warp to.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public EntityUid? NextWarpPoint;

    /// <summary>
    /// Is the ship even able to FTL to another point?
    /// </summary>
    [DataField("stranded"), ViewVariables(VVAccess.ReadOnly)]
    public bool Stranded;

    /// <summary>
    /// The current state of the AI.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public AiStates AiState = AiStates.Cruising;

    /// <summary>
    /// Used to store ships that may not be hostile to the faction however are hostile to the ship itself (independents shooting CG ships will be added here so that the AI has a valid reason to switch into fight mode).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public readonly List<EntityUid> HostileShips = new ();
}
