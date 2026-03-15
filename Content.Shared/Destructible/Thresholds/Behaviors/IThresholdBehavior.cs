using Content.Shared.Database;

namespace Content.Shared.Destructible.Thresholds.Behaviors;

public interface IThresholdBehavior
{
    public LogImpact Impact => LogImpact.Low;

    /// <summary>
    ///     Executes this behavior.
    /// </summary>
    /// <param name="owner">The entity that owns this behavior.</param>
    /// <param name="cause">The entity that caused this behavior.</param>
    void Execute(EntityUid owner, EntityUid? cause = null);
}
