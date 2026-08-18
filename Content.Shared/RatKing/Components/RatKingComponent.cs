using Content.Shared.Dataset;
using Content.Shared.RatKing.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.RatKing.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRatKingSystem))]
[AutoGenerateComponentState]
public sealed partial class RatKingComponent : Component
{
    /// <summary>
    /// The amount of hunger one use of Raise Army consumes.
    /// </summary>
    [DataField(required: true)]
    public float HungerPerArmyUse = 25f;

    /// <summary>
    /// The entity prototype of the mob that Raise Army summons.
    /// </summary>
    [DataField]
    public EntProtoId ArmyMobSpawnId = "MobRatServant";

    /// <summary>
    /// The amount of hunger one use of Domain consumes.
    /// </summary>
    [DataField(required: true)]
    public float HungerPerDomainUse = 50f;

    /// <summary>
    /// How many moles of ammonia are released after one us of Domain.
    /// </summary>
    [DataField]
    public float MolesAmmoniaPerDomain = 200f;

    /// <summary>
    /// The current order that the Rat King assigned.
    /// </summary>
    [DataField("currentOrders")]
    [AutoNetworkedField]
    public RatKingOrderType CurrentOrder = RatKingOrderType.Follow;

    /// <summary>
    /// The servants that the rat king is currently controlling.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Servants = new();

    /// <summary>
    /// A dictionary with an order type to the corresponding callout dataset.
    /// </summary>
    [DataField]
    public Dictionary<RatKingOrderType, ProtoId<LocalizedDatasetPrototype>> OrderCallouts = new()
    {
        { RatKingOrderType.Stay, "RatKingCommandStay" },
        { RatKingOrderType.Follow, "RatKingCommandFollow" },
        { RatKingOrderType.CheeseEm, "RatKingCommandCheeseEm" },
        { RatKingOrderType.Loose, "RatKingCommandLoose" }
    };
}

[Serializable, NetSerializable]
public enum RatKingOrderType : byte
{
    Stay,
    Follow,
    CheeseEm,
    Loose
}
