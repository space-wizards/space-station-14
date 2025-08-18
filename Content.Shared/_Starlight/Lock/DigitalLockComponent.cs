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
    [AutoNetworkedField]
    public string Code = "";

    [AutoNetworkedField]
    public string EnteredCode = "";

    [AutoNetworkedField]
    public DigitalLockStatus Status = DigitalLockStatus.AWAIT_CODE;

    [DataField]
    public int MaxCodeLength = 6;

    public int LastPlayedKeypadSemitones = 0;

    [DataField("keypadPressSound")]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField("accessGrantedSound")]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    [DataField("accessDeniedSound")]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");
}