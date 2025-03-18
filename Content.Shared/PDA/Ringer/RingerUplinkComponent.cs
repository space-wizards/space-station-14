namespace Content.Shared.PDA.Ringer;

/// <summary>
/// Opens the store ui when the ringstone is set to the secret code.
/// Traitors are told the code when greeted.
/// </summary>
[RegisterComponent, Access(typeof(SharedRingerSystem))]
public sealed partial class RingerUplinkComponent : Component
{
    /// <summary>
    /// Notes to set ringtone to in order to lock or unlock the uplink.
    /// Automatically initialized to random notes.
    /// </summary>
    [DataField(serverOnly: true)]
    public Note[] Code = new Note[SharedRingerSystem.RingtoneLength];

    /// <summary>
    /// Whether to show the toggle uplink button in pda settings.
    /// </summary>
    [DataField]
    public bool Unlocked;
}
