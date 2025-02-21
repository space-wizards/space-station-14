using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Revenant.Systems;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevenantSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RevenantComponent : Component
{
    /// <summary>
    ///     The total amount of Essence the revenant has. Functions
    ///     as health and is regenerated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Essence = 75;

    /// <summary>
    ///     The entity's current max amount of <see cref="Essence" />. Can be increased
    ///     through harvesting player souls.
    /// </summary>
    [DataField("maxEssence"), AutoNetworkedField]
    public FixedPoint2 EssenceRegenCap = 75;

    /// <summary>
    ///     The <see cref="CurrencyPrototype" /> to use for the store.
    /// </summary>
    [DataField]
    public ProtoId<CurrencyPrototype> StolenEssenceCurrencyPrototype = "StolenEssence";

    /// <summary>
    ///     <see cref="EntityPrototype" /> to spawn when the entity dies.
    /// </summary>
    [DataField]
    public EntProtoId SpawnOnDeathPrototype = "Ectoplasm";

    /// <summary>
    ///     The coefficient of damage taken to actual health lost.
    /// </summary>
    [DataField]
    public float DamageToEssenceCoefficient = 0.75f;

    /// <summary>
    ///     The amount of <see cref="Essence" /> passively generated per <see cref="UpdateInterval" />.
    /// </summary>
    [DataField]
    public FixedPoint2 EssencePerUpdate = 0.5f;

    /// <summary>
    ///     The <see cref="TimeSpan" /> between updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The <see cref="TimeSpan" /> of the next update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdateTime;

    #region Harvest Ability

    // Here's the gist of the harvest ability:
    // Step 1: The revenant clicks on an entity to "search" for it's soul, which creates a doafter.
    // Step 2: After the doafter is completed, the soul is "found" and can be harvested.
    // Step 3: Clicking the entity again begins to harvest the soul, which causes the revenant to become vulnerable
    // Step 4: The second doafter for the harvest completes, killing the target and granting the revenant essence.

    /// <summary>
    ///     The duration of the soul search
    /// </summary>
    [DataField]
    public TimeSpan SoulSearchDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    ///     The time for which the entity is stunned when harvesting.
    /// </summary>
    [DataField]
    public TimeSpan HarvestStunTime = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The time for which the entity is made solid when harvesting.
    /// </summary>
    [DataField]
    public TimeSpan HarvestCorporealTime = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The amount that is given to the revenant each time it's max essence is upgraded.
    /// </summary>
    [DataField]
    public float MaxEssenceUpgradeAmount = 10;

    #endregion

    [DataField]
    public ProtoId<AlertPrototype> EssenceAlert = "Essence";

    [DataField]
    public EntityUid? Action;
}
