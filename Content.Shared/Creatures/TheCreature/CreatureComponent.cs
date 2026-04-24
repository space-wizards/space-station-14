using Robust.Shared.GameStates;

namespace Content.Shared.Creatures.TheCreature;

/// <summary>
///     Tracks blood pool and evolution state for The Creature antag.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CreatureComponent : Component
{
    /// <summary>Spendable blood right now.</summary>
    [DataField, AutoNetworkedField]
    public int BloodPool = 200;

    /// <summary>Current blood pool ceiling.</summary>
    [DataField, AutoNetworkedField]
    public int MaxBloodPool = 400;

    /// <summary>Lifetime blood consumed this round (for objective tracking).</summary>
    [DataField, AutoNetworkedField]
    public int BloodConsumedTotal = 0;

    /// <summary>Upgrade ID → current rank (0 = unpurchased, max 3).</summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, int> UpgradeRanks = new();
}
