using Robust.Shared.GameStates;

namespace Content.Shared.GhostTypes;

/// <summary>
/// Changes the entity sprite according to damage taken
/// Slash may be shown by cuts and slashes on the ghost, Heat as flames, Cold as frostbite and ice, Radiation as a green glow, etc.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GhostSpriteStateComponent : Component
{
    /// <summary>
    /// Prefix the GhostSpriteStateSystem will add to the name of the damage type it chooses.
    /// It should be identical to the prefix of the entity optional damage sprites.
    /// (Example) Ghosts sprites currently use a "ghost_" prefix for their optional damage states.
    /// </summary>
    [DataField]
    public string Prefix;
}
