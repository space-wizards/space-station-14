using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that passes only if the current round time falls between the minimum and maximum time values.
/// </summary>
public sealed partial class RoundDurationCondition : EntityTableCondition
{
    /// <summary>
    /// Minimum time the round must have gone on for this condition to pass.
    /// </summary>
    [DataField]
    public TimeSpan Min = TimeSpan.Zero;

    /// <summary>
    /// Maximum amount of time the round could go on for this condition to pass.
    /// </summary>
    [DataField]
    public TimeSpan Max = TimeSpan.MaxValue;

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var gameTicker = entMan.System<SharedGameTicker>();
        var duration = gameTicker.RoundDuration();

        return duration >= Min && duration <= Max;
    }
}
