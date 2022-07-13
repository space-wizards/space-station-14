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
    [ViewVariables, DataField("essence")]
    public float Essence = 75;

    [DataField("maxEssence")]
    public float MaxEssence = 75;

    [DataField("essenceEquilibrium")]
    public float EssenceEquilibrium = 75;

    [DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 0.75f;

    [DataField("essencePerSecond")]
    public float EssencePerSecond = 0.5f;

    [DataField("soulSearchDuration")]
    public float SoulSearchDuration = 2.5f;

    [DataField("harvestDuration")]
    public float HarvestDuration = 5;

    public CancellationTokenSource? HarvestCancelToken;

    [ViewVariables]
    public float Accumulator = 0;
}
