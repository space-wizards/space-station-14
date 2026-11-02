using Robust.Shared.GameStates;

namespace Content.Shared.CF.Combat;

/// <summary>
/// The creature is off-guard to melee attacks from the creatures currently flanking it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFOffGuardComponent : Component
{
    /// <summary>Replicated so examine text and the character sheet can show the condition.</summary>
    [AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Server-only. Melee Strikes from these entities take the off-guard AC penalty.
    /// </summary>
    public HashSet<EntityUid> FlankedBy = new();
}
