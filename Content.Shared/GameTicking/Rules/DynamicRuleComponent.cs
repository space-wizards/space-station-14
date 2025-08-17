using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GameTicking.Rules;

/// <summary>
/// Gamerule the spawns multiple antags at intervals based on a budget
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DynamicRuleComponent : Component
{
    /// <summary>
    /// The total budget for antags.
    /// </summary>
    [DataField]
    public float Budget;

    /// <summary>
    /// The last time budget was updated.
    /// </summary>
    [DataField]
    public TimeSpan LastBudgetUpdate;

    /// <summary>
    /// The amount of budget accumulated every second.
    /// </summary>
    [DataField]
    public float BudgetPerSecond = 0.1f;

    /// <summary>
    /// The minimum or lower bound for budgets to start at.
    /// </summary>
    [DataField]
    public int StartingBudgetMin = 200;

    /// <summary>
    /// The maximum or upper bound for budgets to start at.
    /// </summary>
    [DataField]
    public int StartingBudgetMax = 350;

    /// <summary>
    /// The time at which the next rule will start
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRuleTime;

    /// <summary>
    /// Minimum delay between rules
    /// </summary>
    [DataField]
    public TimeSpan MinRuleInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Maximum delay between rules
    /// </summary>
    [DataField]
    public TimeSpan MaxRuleInterval = TimeSpan.FromMinutes(30);

    /// <summary>
    /// A table of rules that are picked from.
    /// </summary>
    [DataField]
    public EntityTableSelector Table = new NoneSelector();

    /// <summary>
    /// The rules that have been spawned
    /// </summary>
    [DataField]
    public List<EntityUid> Rules = new();
}
