using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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

    /// <summary>
    /// Should link damage types names to an int, according to the amount of possible sprites for that specific type.
    /// (The GhostSpriteStateSystem will randomly choose between them)
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, int> DamageMap = new();
}
