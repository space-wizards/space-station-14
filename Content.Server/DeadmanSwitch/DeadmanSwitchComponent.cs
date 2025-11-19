using Robust.Shared.Audio;

namespace Content.Server.DeadmanSwitch;

/// <summary>
/// Component holding the state of a deadman's switch, which is mainly 'armed' or 'disarmed'.
/// </summary>

[RegisterComponent]
public sealed partial class DeadmanSwitchComponent : Component
{
    /// <summary>
    /// Whether the switch is armed or not
    /// </summary>
    [DataField("armed")]
    public bool Armed = false;

    /// <summary>
    /// How long it takes to arm / disarm it
    /// </summary>
    [DataField("armDelay")]
    public float ArmDelay;

    /// <summary>
    /// The sound the switch makes when it flips on or off
    /// </summary>
    [DataField("switchSound")]
    public SoundSpecifier? SwitchSound;
}