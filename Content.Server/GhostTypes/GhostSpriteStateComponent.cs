namespace Content.Server.GhostTypes;

/// <summary>
/// Changes the entity sprite according to damage taken
/// </summary>
[RegisterComponent]
public sealed partial class GhostSpriteStateComponent : Component
{
    /// <summary>
    /// Prefix the GhostSpriteStateSystem will add to the name of the damage type it chooses.
    /// It should be identical to the prefix of the entity optional damage sprites.
    /// </summary>
    [DataField]
    public string Prefix;
}
