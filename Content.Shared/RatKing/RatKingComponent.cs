using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.RatKing;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRatKingSystem))]
[AutoGenerateComponentState]
public sealed partial class RatKingComponent : Component
{
    [DataField]
    public EntProtoId ActionRaiseArmy = "ActionRatKingRaiseArmy";

    /// <summary>
    ///     The action for the Raise Army ability
    /// </summary>
    [DataField("actionRaiseArmyEntity")]
    public EntityUid? ActionRaiseArmyEntity;

    /// <summary>
    ///     The amount of hunger one use of Raise Army consumes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hungerPerArmyUse", required: true)]
    public float HungerPerArmyUse = 25f;

    /// <summary>
    ///     The entity prototype of the mob that Raise Army summons
    /// </summary>
    [DataField]
    public EntProtoId ArmyMobSpawnId = "MobRatServant";

    [DataField]
    public EntProtoId ActionDomain = "ActionRatKingDomain";

    /// <summary>
    ///     The action for the Domain ability
    /// </summary>
    [DataField("actionDomainEntity")]
    public EntityUid? ActionDomainEntity;

    /// <summary>
    ///     The amount of hunger one use of Domain consumes
    /// </summary>
    [DataField("hungerPerDomainUse", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float HungerPerDomainUse = 50f;

    /// <summary>
    ///     How many moles of ammonia are released after one us of Domain
    /// </summary>
    [DataField("molesAmmoniaPerDomain"), ViewVariables(VVAccess.ReadWrite)]
    public float MolesAmmoniaPerDomain = 200f;

    /// <summary>
    /// The current order that the Rat King assigned.
    /// </summary>
    [DataField("currentOrders"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public RatKingOrderType CurrentOrder = RatKingOrderType.Loose;

    /// <summary>
    /// The servants that the rat king is currently controlling
    /// </summary>
    [DataField("servants")]
    public HashSet<EntityUid> Servants = new();

    [DataField]
    public EntProtoId ActionOrderStay = "ActionRatKingOrderStay";

    [DataField("actionOrderStayEntity")]
    public EntityUid? ActionOrderStayEntity;

    [DataField]
    public EntProtoId ActionOrderFollow = "ActionRatKingOrderFollow";

    [DataField("actionOrderFollowEntity")]
    public EntityUid? ActionOrderFollowEntity;

    [DataField]
    public EntProtoId ActionOrderCheeseEm = "ActionRatKingOrderCheeseEm";

    [DataField("actionOrderCheeseEmEntity")]
    public EntityUid? ActionOrderCheeseEmEntity;

    [DataField]
    public EntProtoId ActionOrderLoose = "ActionRatKingOrderLoose";

    [DataField("actionOrderLooseEntity")]
    public EntityUid? ActionOrderLooseEntity;

    /// <summary>
    /// A dictionary with an order type to the corresponding callout dataset.
    /// </summary>
    [DataField("orderCallouts")]
    public Dictionary<RatKingOrderType, string> OrderCallouts = new()
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
