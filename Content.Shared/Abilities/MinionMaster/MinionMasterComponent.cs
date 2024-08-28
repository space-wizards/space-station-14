using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.ComponentModel.DataAnnotations;

namespace Content.Shared.Abilities.MinionMaster;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMinionMasterSystem))]
[AutoGenerateComponentState]
public sealed partial class MinionMasterComponent : Component
{
    [DataField("actionRaiseArmy", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionRaiseArmy = "ActionMinionMasterRaiseArmy";

    /// <summary>
    ///     The action for the Raise Army ability
    /// </summary>
    [DataField("actionRaiseArmyEntity")]
    public EntityUid? ActionRaiseArmyEntity;

    /// <summary>
    ///     Whether or not summoning minions costs hunger.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("doesSummonCostFood", required: true)]
    public bool DoesSummonCostFood = true;

    /// <summary>
    ///     The amount of hunger one use of Raise Army consumes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hungerPerArmyUse")]
    public float HungerPerArmyUse = 25f;

    /// <summary>
    ///     The entity prototype of the mob that Raise Army summons.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("armyMobSpawnId", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ArmyMobSpawnId = "MobRatServant";

    /// <summary>
    /// The current order that the Rat King assigned.
    /// </summary>
    [DataField("currentOrders"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public MinionOrderType CurrentOrder = MinionOrderType.Loose;

    /// <summary>
    /// The minions that the minion master is currently controlling
    /// </summary>
    [DataField("servants")]
    public HashSet<EntityUid> Minions = new();

    [DataField("actionOrderStay", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderStay = "ActionMinionMasterOrderStay";

    [DataField("actionOrderStayEntity")]
    public EntityUid? ActionOrderStayEntity;

    [DataField("actionOrderFollow", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderFollow = "ActionMinionMasterOrderFollow";

    [DataField("actionOrderFollowEntity")]
    public EntityUid? ActionOrderFollowEntity;

    [DataField("actionOrderAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderCheeseEm = "ActionMinionMasterOrderAttack";

    [DataField("actionOrderAttackEntity")]
    public EntityUid? ActionOrderAttackEntity;

    [DataField("actionOrderLoose", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderLoose = "ActionMinionMasterOrderLoose";

    [DataField("actionOrderLooseEntity")]
    public EntityUid? ActionOrderLooseEntity;

    /// <summary>
    /// A dictionary with an order type to the corresponding callout dataset.
    /// </summary>
    [DataField("orderCallouts")]
    public Dictionary<MinionOrderType, string> OrderCallouts = new()
    {
        { MinionOrderType.Stay, "RatKingCommandStay" },
        { MinionOrderType.Follow, "RatKingCommandFollow" },
        { MinionOrderType.Attack, "RatKingCommandCheeseEm" },
        { MinionOrderType.Loose, "RatKingCommandLoose" }
    };
}

[Serializable, NetSerializable]
public enum MinionOrderType : byte
{
    Stay,
    Follow,
    Attack,
    Loose
}
