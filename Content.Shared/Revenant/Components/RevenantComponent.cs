using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevenantComponent : Component
{
    [DataField]
    public ProtoId<CurrencyPrototype> StolenEssenceCurrencyPrototype = "StolenEssence";

    [DataField]
    public ProtoId<SatiationTypePrototype> EssenceSatiationPrototype = "Essence";

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField]
    public EntProtoId SpawnOnDeathPrototype = "Ectoplasm";

    /// <summary>
    /// The coefficient of damage taken to actual health lost.
    /// </summary>
    [DataField]
    public float DamageToEssenceCoefficient = 0.75f;

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
    public int HarvestStunDuration = 5;

    [DataField]
    public int HarvestCorporalDuration = 5;


    /// <summary>
    /// The amount that is given to the revenant each time it's max essence is upgraded.
    /// </summary>
    [DataField]
    public int MaxEssenceUpgradeAmount = 10;

    [DataField]
    public SatiationValue MinEssenceThreshold = "Empty";
    #endregion

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
