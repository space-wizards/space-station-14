using Content.Shared.Actions.ActionTypes;
using Content.Shared.Disease;
using Content.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using System.Threading;

namespace Content.Server.Revenant;

[RegisterComponent]
public sealed class RevenantComponent : Component
{
    /// <summary>
    /// The total amount of Essence the revenant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Essence = 75;

    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public float MaxEssence = 75;

    [DataField("essenceCap")]
    public float EssenceCap = 150;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("essencePerSecond")]
    public float EssencePerSecond = 0.25f;

    [ViewVariables]
    public float Accumulator = 0;

    #region Harvest Ability
    [DataField("soulSearchDuration")] public float SoulSearchDuration = 2.5f;
    [DataField("harvestDuration")] public float HarvestDuration = 5;
    [DataField("perfectSoulChance")] public float PerfectSoulChance = 0.35f;
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")] public float MaxEssenceUpgradeAmount = 10;

    public CancellationTokenSource? HarvestCancelToken;
    #endregion

    #region Defile Ability
    public float DefileUseCost = -30;
    public float DefileStunDuration = 1;
    public float DefileCorporealDuration = 4;
    public float DefileRadius = 3.5f;
    public int DefileTilePryAmount = 15;
    public float DefileEffectChance = 0.5f;
    #endregion

    #region Malfunction Ability
    public float MalfuncitonUseCost = -60;
    public float MalfunctionStunDuration = 2;
    public float MalfunctionCorporealDuration = 8;
    public float MalfunctionRadius = 3.5f;
    public float MalfunctionEffectChance = 0.5f;
    #endregion
}
