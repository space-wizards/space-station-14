using Content.Shared.Beeper.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.ProximityDetection.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Beeper.Components;

/// <summary>
/// This is used for an item that beeps based on
/// proximity to a specified component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BeeperSystem)), AutoGenerateComponentState]
public sealed partial class BeeperComponent : Component
{
    /// <summary>
    /// Whether or not it's on.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    /// How much to scale the interval by (< 0 = min, > 1 = max)
    /// </summary>
    [DataField("intervalScaling"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 IntervalScaling = 0;

    /// <summary>
    /// The maximum interval between beeps.
    /// </summary>
    [DataField("maxBeepInterval"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MaxBeepInterval = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The minimum interval between beeps.
    /// </summary>
    [DataField("minBeepInterval"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MinBeepInterval = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// Interval for the next beep
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Interval;

    /// <summary>
    /// Time when we beeped last
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastBeepTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextBeep => LastBeepTime == TimeSpan.MaxValue ? TimeSpan.MaxValue : LastBeepTime + Interval;

    /// <summary>
    /// Is the beep muted
    /// </summary>
    [DataField("muted"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsMuted = false;

    /// <summary>
    /// The sound played when the locator beeps.
    /// </summary>
    [DataField("beepSound"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public SoundSpecifier? BeepSound;
}
