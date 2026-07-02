using Robust.Shared.GameStates;

namespace Content.Shared.Creatures.SpaceLeech;

/// <summary>
///     Tracks blood pool and evolution state for the Space Leech antag.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpaceLeechComponent : Component
{
    /// <summary>Spendable blood right now. Stored as float to preserve fractional units from impure blood.</summary>
    [DataField, AutoNetworkedField]
    public float BloodPool = 0f;

    /// <summary>Current blood pool ceiling.</summary>
    [DataField, AutoNetworkedField]
    public int MaxBloodPool = 400;

    /// <summary>Lifetime blood consumed this round (for objective tracking).</summary>
    [DataField, AutoNetworkedField]
    public float BloodConsumedTotal = 0f;

    /// <summary>Upgrade ID → current rank (0 = unpurchased, max 3).</summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, int> UpgradeRanks = new();

}
