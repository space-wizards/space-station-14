using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Creatures.SpaceLeech;

/// <summary>
///     Tracks blood pool and evolution state for the Space Leech antag.
///     Server-authoritative; networked so the client can drive the upgrade menu
///     and predict movement/melee modifiers from the purchased ranks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpaceLeechComponent : Component
{
    /// <summary>Spendable blood right now.</summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodPool = FixedPoint2.Zero;

    /// <summary>Current blood pool ceiling.</summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxBloodPool = 400;

    /// <summary>Lifetime blood consumed this round (for objective tracking). Not capped by the pool.</summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodConsumedTotal = FixedPoint2.Zero;

    /// <summary>Upgrade ID → current rank (absent/0 = unpurchased).</summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<SpaceLeechUpgradePrototype>, int> UpgradeRanks = new();

    /// <summary>
    /// Fraction of consumed blood that restores the leech's own bloodstream directly,
    /// bypassing metabolism so its own blood reagent is never injected.
    /// </summary>
    [DataField]
    public float BloodRestoreFraction = 0.2f;

    /// <summary>Sting action granted once the Venom upgrade reaches rank 1.</summary>
    [DataField]
    public EntProtoId StingAction = "ActionSpaceLeechSting";

    /// <summary>The granted sting action entity, if any.</summary>
    [DataField]
    public EntityUid? StingActionEntity;
}
