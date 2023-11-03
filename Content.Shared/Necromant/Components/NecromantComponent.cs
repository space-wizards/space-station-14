using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Necromant.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecromantComponent : Component
{

    /// <summary>
    /// The total amount of Essence the Necromant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Essence = 75;

    [DataField("stolenEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenEssenceCurrencyPrototype = "StolenEssence";

    /// <summary>
    /// The entity's current max amount of Essence. Can be increased
    /// through harvesting player Essence.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 75;

    /// <summary>
    /// The coefficient of damage taken to actual health lost.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 0.5f;

    /// <summary>
    /// The amount of Essence passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("essencePerSecond")]
    public FixedPoint2 EssencePerSecond = 0.5f;

    [ViewVariables]
    public float Accumulator = 0;

    // Here's the gist of the harvest ability:
    // Step 1: The Necromant clicks on an entity to "search" for it's soul, which creates a doafter.
    // Step 2: After the doafter is completed, the soul is "found" and can be harvested.
    // Step 3: Clicking the entity again begins to harvest the soul, which causes the Necromant to become vulnerable
    // Step 4: The second doafter for the harvest completes, killing the target and granting the Necromant Essence.
    #region Harvest Ability


    /// <summary>
    /// The duration of the soul search
    /// </summary>
    [DataField("soulSearchDuration")]
    public float SoulSearchDuration = 2.5f;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("harvestDebuffs")]
    public Vector2 HarvestDebuffs = new(5);

    /// <summary>
    /// The amount that is given to the Necromant each time it's max Essence is upgraded.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")]
    public float MaxEssenceUpgradeAmount = 10;
    #endregion

    //In the nearby radius, causes various objects to be thrown, messed with, and containers opened
    //Generally just causes a mess
    #region Defile Ability
    /// <summary>
    /// The amount of Essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileCost")]
    public FixedPoint2 DefileCost = -30;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("defileDebuffs")]
    public Vector2 DefileDebuffs = new(1);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileRadius")]
    public float DefileRadius = 3.5f;

    /// <summary>
    /// The amount of tiles that are uprooted by the ability
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileTilePryAmount")]
    public int DefileTilePryAmount = 15;

    /// <summary>
    /// The chance that an individual entity will have any of the effects
    /// happen to it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileEffectChance")]
    public float DefileEffectChance = 0.5f;
    #endregion


        #region Army Ability
        [DataField("actionNecromantRaiseArmy", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionRaiseArmy = "ActionNecromantRaiseArmy";

        /// <summary>
        ///     The action for the Raise Army ability
        /// </summary>
        [DataField("actionNecromantRaiseArmyEntity")] public EntityUid? ActionRaiseArmyEntity;
        
        [ViewVariables(VVAccess.ReadWrite), DataField("armyCost")]
        public FixedPoint2 ArmyCost = -30;

        [DataField("armyDebuffs")]
        public Vector2 ArmyDebuffs = new(3);
        /// <summary>
        ///     The entity prototype of the mob that Raise Army summons
        /// </summary>


        [ViewVariables(VVAccess.ReadWrite), DataField("armyMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ArmyMobSpawnId = "MobSlasher";

        

        [DataField("actionNecromantRaisePregnantEntity")] public EntityUid? ActionRaisePregnantEntity;
        
        [ViewVariables(VVAccess.ReadWrite), DataField("PregnantCost")]
        public FixedPoint2 PregnantCost = -60;

        [DataField("actionNecromantRaisePregnant", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionRaisePregnant = "ActionNecromantRaisePregnant";


        [ViewVariables(VVAccess.ReadWrite), DataField("PregnantMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PregnantMobSpawnId = "MobPregnant";


        [DataField("actionNecromantRaiseInfectorEntity")] public EntityUid? ActionRaiseInfectorEntity;
        
        [ViewVariables(VVAccess.ReadWrite), DataField("InfectorCost")]
        public FixedPoint2 InfectorCost = -120;
        
        [DataField("actionNecromantRaiseInfector", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionRaiseInfector = "ActionNecromantRaiseInfector";
        [ViewVariables(VVAccess.ReadWrite), DataField("InfectorMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string InfectorMobSpawnId = "MobInfector";

        [DataField("actionNecromantRaiseTwitcherEntity")] public EntityUid? ActionRaiseTwitcherEntity;

        [ViewVariables(VVAccess.ReadWrite), DataField("TwitcherCost")]
        public FixedPoint2 TwitcherCost = -60;

        [DataField("actionNecromantRaiseTwitcher", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionRaiseTwitcher = "ActionNecromantRaiseTwitcher";

        [ViewVariables(VVAccess.ReadWrite), DataField("TwitcherMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string TwitcherMobSpawnId = "MobTwitcher";


        [DataField("actionNecromantRaiseDivaderEntity")] public EntityUid? ActionRaiseDivaderEntity;

        [ViewVariables(VVAccess.ReadWrite), DataField("DivaderCost")]
        public FixedPoint2 DivaderCost = -80;

        [DataField("actionNecromantRaiseDivader", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionRaiseDivader = "ActionNecromantRaiseDivader";

        [ViewVariables(VVAccess.ReadWrite), DataField("DivaderMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string DivaderMobSpawnId = "MobDivader";


        [DataField("actionNecromantRaiseBruteEntity")] public EntityUid? ActionRaiseBruteEntity;

        [ViewVariables(VVAccess.ReadWrite), DataField("DivaderBrute")]
        public FixedPoint2 BruteCost = -180;

        [DataField("actionNecromantRaiseBrute", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionRaiseBrute = "ActionNecromantRaiseBrute";

        [ViewVariables(VVAccess.ReadWrite), DataField("BruteMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BruteMobSpawnId = "MobBrute";

        #endregion

}
