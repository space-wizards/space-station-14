using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BureaucraticErrorRule))]
public sealed partial class BureaucraticErrorRuleComponent : Component
{
    /// <summary>
    /// The jobs that are ignored by this rule and won't have their slots changed.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> IgnoredJobs = new();

    /// <summary>
    /// Chance of rolling the event subtype which closes all roles besides one which now has infinite slots.
    /// </summary>
    [DataField]
    public float CloseAllButOneChance = 0.25f;

    /// <summary>
    /// The proportion of jobs to adjust slots of in the 'adjust individual jobs' variant of the rule.
    /// </summary>
    [DataField]
    public MinMax ProportionOfJobsToAdjust = new MinMax(0.2f, 0.3f);

    /// <summary>
    /// Minimum/maximum slot quantity adjustment for the jobs which get adjusted by this rule.
    /// </summary>
    [DataField]
    public MinMax JobSlotAdjustment = new MinMax(-3, 5);
}
