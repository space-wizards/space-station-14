namespace Content.Server.GhostTypes;

/// <summary>
/// Changes the entity sprite according to damage taken
/// </summary>
[RegisterComponent]
public sealed partial class GhostSpriteStateComponent : Component
{
    /// <summary>
    /// Prefix the system will add to the damage name it's using
    /// </summary>
    [DataField]
    public string Prefix;
}
