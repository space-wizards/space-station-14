using Content.Shared.Disease;
using Content.Shared.Revenant;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Threading;
using Content.Shared.FixedPoint;

namespace Content.Server.Revenant;

[RegisterComponent]
public sealed class RevenantComponent : SharedRevenantComponent
{
    /// <summary>
    /// The total amount of Essence the revenant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Essence = 75;

    /// <summary>
    /// Used for purchasing shop items.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 StolenEssence = 0;

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 75;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("essencePerSecond")]
    public FixedPoint2 EssencePerSecond = 0.25f;

    [ViewVariables]
    public float Accumulator = 0;

    #region Harvest Ability
    [ViewVariables, DataField("soulSearchDuration")]
    public float SoulSearchDuration = 2.5f;

    [ViewVariables, DataField("harvestDebuffs")]
    public Vector2 HarvestDebuffs = (5, 5);

    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")]
    public float MaxEssenceUpgradeAmount = 10;

    public CancellationTokenSource? SoulSearchCancelToken;
    public CancellationTokenSource? HarvestCancelToken;
    #endregion

    #region Defile Ability
    [ViewVariables(VVAccess.ReadWrite), DataField("defileCost")]
    public FixedPoint2 DefileCost = -30;

    [ViewVariables, DataField("defileDebuffs")]
    public Vector2 DefileDebuffs = (1, 4);

    [ViewVariables(VVAccess.ReadWrite), DataField("defileRadius")]
    public float DefileRadius = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("defileTilePryAmount")]
    public int DefileTilePryAmount = 15;

    [ViewVariables(VVAccess.ReadWrite), DataField("defileEffectChance")]
    public float DefileEffectChance = 0.5f;
    #endregion

    #region Overload Lights Ability
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadCost")]
    public FixedPoint2 OverloadCost = -40;

    [ViewVariables, DataField("overloadDebuffs")]
    public Vector2 OverloadDebuffs = (3, 8);

    [ViewVariables(VVAccess.ReadWrite), DataField("overloadRadius")]
    public float OverloadRadius = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField("overloadBreakChance")]
    public float OverloadBreakChance = 0.5f;
    #endregion

    #region Blight Ability
    [ViewVariables(VVAccess.ReadWrite), DataField("blightCost")]
    public float BlightCost = -50;

    [ViewVariables, DataField("blightDebuffs")]
    public Vector2 BlightDebuffs = (2, 5);

    [ViewVariables(VVAccess.ReadWrite), DataField("blightRadius")]
    public float BlightRadius = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("blightDiseasePrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
    public string BlightDiseasePrototypeId = "SpectralTiredness";
    #endregion

    #region Malfunction Ability
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionCost")]
    public FixedPoint2 MalfunctionCost = -60;

    [ViewVariables, DataField("malfunctionDebuffs")]
    public Vector2 MalfunctionDebuffs = (2, 8);

    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionRadius")]
    public float MalfunctionRadius = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionEffectChance")]
    public float MalfunctionEffectChance = 0.5f;
    #endregion

    /// <summary>
    /// Stores all of the currently unlockable abilities in the shop.
    /// </summary>
    [ViewVariables]
    public Dictionary<RevenantStoreListingPrototype, bool> Listings = new ();
}

public sealed class SoulSearchDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public SoulSearchDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public HarvestDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs { }
