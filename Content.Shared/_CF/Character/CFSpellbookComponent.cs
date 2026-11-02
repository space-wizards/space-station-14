using Robust.Shared.GameStates;

namespace Content.Shared.CF.Character;

/// <summary>
/// Spell slots and focus points copied from the class at spawn.
/// Index 0 of <see cref="Slots"/> is cantrips. Index 1 is rank 1.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFSpellbookComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<int> Slots = new();

    /// <summary>Expended slots per rank, same indexing as <see cref="Slots"/>.</summary>
    [DataField, AutoNetworkedField]
    public List<int> Expended = new();

    [DataField, AutoNetworkedField]
    public int FocusPoints;

    [DataField, AutoNetworkedField]
    public int FocusSpent;
}
