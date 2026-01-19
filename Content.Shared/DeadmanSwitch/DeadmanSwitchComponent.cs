using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.DeadmanSwitch;

/// <summary>
/// Component holding the state of a deadman's switch, which is mainly 'armed' or 'disarmed'.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeadmanSwitchComponent : Component
{
    /// <summary>
    /// Whether the switch is armed or not
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Armed = false;

    /// <summary>
    /// How long it takes to arm / disarm it
    /// </summary>
    [DataField]
    public float ArmDelay;

    /// <summary>
    /// The sound the switch makes when it flips on or off
    /// </summary>
    [DataField]
    public SoundSpecifier? SwitchSound;

    /// <summary>
    /// At this distance, the deadman's switch triggers linked explosives instantly, bypassing timers.
    /// </summary>
    [DataField]
    public float InstantTriggerRange;
}
