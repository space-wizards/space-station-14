using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevenantComponent : Component
{
    /// <summary>
    /// The total amount of Essence the revenant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Essence = 75;

    [DataField("stolenEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenEssenceCurrencyPrototype = "StolenEssence";

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField("spawnOnDeathPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "Ectoplasm";

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 75;

    /// <summary>
    /// The coefficient of damage taken to actual health lost.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 0.75f;

    /// <summary>
    /// The amount of essence passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("essencePerSecond")]
    public FixedPoint2 EssencePerSecond = 0.5f;

    [ViewVariables]
    public float Accumulator = 0;

    // Here's the gist of the harvest ability:
    // Step 1: The revenant clicks on an entity to "search" for it's soul, which creates a doafter.
    // Step 2: After the doafter is completed, the soul is "found" and can be harvested.
    // Step 3: Clicking the entity again begins to harvest the soul, which causes the revenant to become vulnerable
    // Step 4: The second doafter for the harvest completes, killing the target and granting the revenant essence.
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
    public Vector2 HarvestDebuffs = new(5, 5);

    /// <summary>
    /// The amount that is given to the revenant each time it's max essence is upgraded.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")]
    public float MaxEssenceUpgradeAmount = 10;
    #endregion

    //In the nearby radius, causes various objects to be thrown, messed with, and containers opened
    //Generally just causes a mess
    #region Defile Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileCost")]
    public FixedPoint2 DefileCost = -30;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("defileDebuffs")]
    public Vector2 DefileDebuffs = new(1, 4);

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

    #region Overload Lights Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadCost")]
    public FixedPoint2 OverloadCost = -40;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("overloadDebuffs")]
    public Vector2 OverloadDebuffs = new(3, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadRadius")]
    public float OverloadRadius = 5f;

    /// <summary>
    /// How close to the light the entity has to be in order to be zapped.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadZapRadius")]
    public float OverloadZapRadius = 2f;
    #endregion

    #region Blight Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blightCost")]
    public float BlightCost = -50;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("blightDebuffs")]
    public Vector2 BlightDebuffs = new(2, 5);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blightRadius")]
    public float BlightRadius = 3.5f;
    #endregion

    #region Malfunction Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionCost")]
    public FixedPoint2 MalfunctionCost = -60;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("malfunctionDebuffs")]
    public Vector2 MalfunctionDebuffs = new(2, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionRadius")]
    public float MalfunctionRadius = 3.5f;
    #endregion

    #region Visualizer
    [DataField("state")]
    public string State = "idle";
    [DataField("corporealState")]
    public string CorporealState = "active";
    [DataField("stunnedState")]
    public string StunnedState = "stunned";
    [DataField("harvestingState")]
    public string HarvestingState = "harvesting";
    #endregion

    [DataField] public EntityUid? Action;
}
