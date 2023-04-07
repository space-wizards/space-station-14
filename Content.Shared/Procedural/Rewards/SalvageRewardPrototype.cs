using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Rewards;

/// <summary>
/// Given after successful completion of a salvage mission.
/// </summary>
[Prototype("salvageReward")]
public sealed class SalvageRewardPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("reward", required: true)] public ISalvageReward Reward = default!;
}
