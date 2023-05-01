using Content.Shared.PDA;

namespace Content.Server.PDA.Ringer;

/// <summary>
/// Opens the store ui when the ringstone is set to the secret code.
/// Traitors are told the code when greeted.
/// </summary>
[RegisterComponent, Access(typeof(RingerSystem))]
public sealed class RingerUplinkComponent : Component
{
    /// <summary>
    /// Notes to set ringtone to in order to open the uplink.
    /// Automatically initialized to random notes.
    /// </summary>
    [DataField("code")]
    public Note[] Code = new Note[RingerSystem.RingtoneLength];
}
