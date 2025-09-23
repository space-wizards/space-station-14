using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Lock;

/// <summary>
/// Allows locking/unlocking, with password
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DigitalLockComponent : Component
{
    /// <summary>
    /// Password of the digital lock
    /// </summary>
    [DataField]
    public string Code = "";

    /// <summary>
    /// Determines, is the service panel open
    /// </summary>

    [DataField, AutoNetworkedField]
    public bool MaintenanceOpen = false;

    /// <summary>
    /// Entered code by user
    /// </summary>
    [AutoNetworkedField]
    public string EnteredCode = "";

    /// <summary>
    /// Current lock status
    /// </summary>
    [AutoNetworkedField]
    public DigitalLockStatus Status = DigitalLockStatus.AWAIT_CODE;

    /// <summary>
    /// Max code lenght which can be setuped
    /// </summary>
    [DataField]
    public int MaxCodeLength = 6;

    /// <summary>
    /// Stores last played note of keypad button
    /// </summary>
    public int LastPlayedKeypadSemitones = 0;

    /// <summary>
    /// Sound which played when press number keypad button
    /// </summary>
    [DataField]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    /// <summary>
    /// Sound which played when access granted
    /// </summary>
    [DataField]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    /// <summary>
    /// Sound which played when password wrong
    /// </summary>
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    /// <summary>
    /// Tool quality for Open Maintenance Panel
    /// </summary>
    [DataField]
    public string OpenQuality = "Screwing";

    /// <summary>
    /// Tool quality for Reset password
    /// </summary>
    [DataField]
    public string ResetQuality = "Pulsing";
}