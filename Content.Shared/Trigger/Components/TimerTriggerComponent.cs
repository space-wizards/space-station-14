using Content.Shared.Guidebook;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Linq;

namespace Content.Shared.Trigger.Components;

/// <summary>
/// Starts a timer when activated by a trigger.
/// Will cause a different trigger once the time is over.
/// Can play a sound while the timer is active.
/// The time can be set by other components, for example <see cref="RandomTimerTriggerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TimerTriggerComponent : Component
{
    /// <summary>
    /// The keys that will activate the timer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> KeysIn = new() { TriggerSystem.DefaultTriggerKey };

    /// <summary>
    /// The key that will trigger once the timer is finished.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? KeyOut = "timer";

    /// <summary>
    /// The time after which this timer will trigger after it is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// If not empty, a user can use verbs to configure the delay to one of these options.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<TimeSpan> DelayOptions = new();

    /// <summary>
    /// The time at which this trigger will activate.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTrigger = TimeSpan.Zero;

    /// <summary>
    /// Time of the next beeping sound.
    /// </summary>
    /// <remarks>
    /// Not networked because it's only used server side.
    /// </remarks>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextBeep = TimeSpan.Zero;

    /// <summary>
    /// Initial beep delay.
    /// Defaults to a single BeepInterval if null.
    /// </summary>
    /// <remarks>
    /// Not networked because it's only used server side.
    /// </remarks>
    [DataField]
    public TimeSpan? InitialBeepDelay;

    /// <summary>
    /// The time between beeps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BeepInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The entity that activated this trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// The beeping sound, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? BeepSound;

    /// <summary>
    /// Whether you can examine the item to see its timer or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Examinable = true;

    /// <summary>
    /// The popup to show the user when starting the timer, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Popup = "timer-trigger-activated";

    #region GuidebookData

    [GuidebookData]
    public float? ShortestDelayOption => DelayOptions.Count == 0 ? null : (float)DelayOptions.Min().TotalSeconds;

    [GuidebookData]
    public float? LongestDelayOption => DelayOptions.Count == 0 ? null : (float)DelayOptions.Max().TotalSeconds;

    #endregion GuidebookData
}
