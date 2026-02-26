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

        var duration = TimeSpan.Zero;

        // We can't just use gameTicker.RoundDuration() because when we add roundstart GameRules, we're in the lobby,
        // which means the RoundStartTimeSpan doesn't have the start of the round yet
        if (gameTicker.RoundStartTimeSpan != TimeSpan.Zero)
            duration = gameTicker.RoundDuration();

        return duration >= Min && duration <= Max;
    }
}
