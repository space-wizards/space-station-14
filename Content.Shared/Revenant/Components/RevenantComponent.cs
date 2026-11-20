using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RevenantComponent : Component
{
    /// <summary>
    /// The total amount of Essence the revenant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Essence = 75;

    [DataField]
    public ProtoId<CurrencyPrototype> StolenEssenceCurrencyPrototype = "StolenEssence";

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField]
    public EntProtoId SpawnOnDeathPrototype = "Ectoplasm";

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 75;

    /// <summary>
    /// The coefficient of damage taken to actual health lost.
    /// </summary>
    [DataField]
    public float DamageToEssenceCoefficient = 0.75f;

    /// <summary>
    /// The amount of essence passively generated per second.
    /// </summary>
    [DataField]
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
    [DataField]
    public float SoulSearchDuration = 2.5f;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField]
    public Vector2 HarvestDebuffs = new(5, 5);

    /// <summary>
    /// The amount that is given to the revenant each time it's max essence is upgraded.
    /// </summary>
    [DataField]
    public float MaxEssenceUpgradeAmount = 10;
    #endregion

    //In the nearby radius, causes various objects to be thrown, messed with, and containers opened
    //Generally just causes a mess
    #region Defile Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [DataField]
    public FixedPoint2 DefileCost = 30;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField]
    public Vector2 DefileDebuffs = new(1, 4);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField]
    public float DefileRadius = 3.5f;

    /// <summary>
    /// The amount of tiles that are uprooted by the ability
    /// </summary>
    [DataField]
    public int DefileTilePryAmount = 15;

    /// <summary>
    /// The chance that an individual entity will have any of the effects
    /// happen to it.
    /// </summary>
    [DataField]
    public float DefileEffectChance = 0.5f;
    #endregion

    #region Overload Lights Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [DataField]
    public FixedPoint2 OverloadCost = 40;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField]
    public Vector2 OverloadDebuffs = new(3, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField]
    public float OverloadRadius = 5f;

    /// <summary>
    /// How close to the light the entity has to be in order to be zapped.
    /// </summary>
    [DataField]
    public float OverloadZapRadius = 2f;
    #endregion

    #region Blight Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [DataField]
    public float BlightCost = 50;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField]
    public Vector2 BlightDebuffs = new(2, 5);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField]
    public float BlightRadius = 3.5f;
    #endregion

    #region Malfunction Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [DataField]
    public FixedPoint2 MalfunctionCost = 60;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField]
    public Vector2 MalfunctionDebuffs = new(2, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField]
    public float MalfunctionRadius = 3.5f;

    /// <summary>
    /// Whitelist for entities that can be emagged by malfunction.
    /// Used to prevent ultra gamer things like ghost emagging chem or instantly launching the shuttle.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionWhitelist;

    /// <summary>
    /// Whitelist for entities that can never be emagged by malfunction.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionBlacklist;
    #endregion

    [DataField]
    public ProtoId<AlertPrototype> EssenceAlert = "Essence";

    #region Visualizer
    [DataField]
    public string State = "idle";
    [DataField]
    public string CorporealState = "active";
    [DataField]
    public string StunnedState = "stunned";
    [DataField]
    public string HarvestingState = "harvesting";
    #endregion

    /// <summary>
    /// The scaling for passively chilling surroundings.
    /// </summary>
    [DataField]
    public FixedPoint2 ChillScaling = 7000;

    /// <summary>
    /// The upper limit for essence when passively chilling surroundings.
    /// Beyond this point, more essence will not cause more chilling.
    /// </summary>
    [DataField]
    public FixedPoint2 ChillUpperBound = 500;
}
