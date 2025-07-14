using System.Linq;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components;

/// <summary>
/// Adds verbs to set the duration of a <see cref="TimerTriggerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConfigurableTimerTriggerComponent : Component
{
    /// <summary>
    /// If not null, a user can use verbs to configure the delay to one of these options.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<TimeSpan> DelayOptions = new() { TimeSpan.FromSeconds(1) };

    #region GuidebookData

    [GuidebookData]
    public TimeSpan? ShortestDelayOption => DelayOptions.Min();

    [GuidebookData]
    public TimeSpan? LongestDelayOption => DelayOptions.Max();

    #endregion GuidebookData
}
