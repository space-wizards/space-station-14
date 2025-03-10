using Robust.Shared.Prototypes;

namespace Content.Shared.RoundStatistics;

/// <summary>
/// Used to create round statistics
/// </summary>
[Serializable, Prototype("roundStatistic")]
public sealed class RoundStatisticPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    /// String that will be shown in round end window
    /// </summary>
    [DataField(required: true)]
    public LocId StatString;
}
