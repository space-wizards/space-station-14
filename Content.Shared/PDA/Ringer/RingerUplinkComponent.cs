using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Ringer;

/// <summary>
/// Opens the store UI when the ringstone is set to the secret code.
/// Traitors are told the code when greeted.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRingerSystem))]
public sealed partial class RingerUplinkComponent : Component
{
    /// <summary>
    /// Notes to set ringtone to in order to lock or unlock the uplink.
    /// Set via GenerateUplinkCodeEvent.
    /// </summary>
    [DataField]
    public Note[] Code = new Note[SharedRingerSystem.RingtoneLength];

    /// <summary>
    /// Whether to show the toggle uplink button in PDA settings.
    /// </summary>
    [DataField]
    public bool Unlocked;
}
