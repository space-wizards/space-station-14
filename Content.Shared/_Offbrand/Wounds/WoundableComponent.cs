using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(WoundableSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class WoundableComponent : Component
{
    /// <summary>
    /// The total damages accumulated on this woundable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// The total damages accumulated on this organ from tended wounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier TendedDamage = new();

    /// <summary>
    /// The map of damage types and ranges to wound entities.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, SortedDictionary<FixedPoint2, EntProtoId<WoundComponent>>> PotentialWounds;
}

/// <summary>
/// Raised on a woundable when its wound damage changes
/// </summary>
[ByRefEvent]
public readonly record struct WoundableDamageChanged;

/// <summary>
/// Raised when users need to refresh cached data on wounds.
/// </summary>
/// <param name="InterruptsDoAfters">Whether this should interrupt do-afters.</param>
/// <param name="Origin">The entity that caused wounds to be refreshed.</param>
[ByRefEvent]
public readonly record struct RefreshWoundsEvent(bool InterruptsDoAfters, EntityUid? Origin);
