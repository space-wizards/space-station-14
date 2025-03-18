using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Ringer;

[RegisterComponent, NetworkedComponent]
public sealed partial class RingerComponent : Component
{
    /// <summary>
    /// The ringtone, represented as an array of notes.
    /// </summary>
    [DataField]
    public Note[] Ringtone = new Note[SharedRingerSystem.RingtoneLength];

    /// <summary>
    /// The last time this ringer's ringtone was set.
    /// </summary>
    [DataField]
    public TimeSpan LastRingtoneSetTime;

    /// <summary>
    /// The time when the next note should play.
    /// </summary>
    [DataField]
    public TimeSpan? NextNoteTime;

    /// <summary>
    /// Keeps track of how many notes have elapsed if the ringer component is playing.
    /// </summary>
    [DataField]
    public int NoteCount;

    /// <summary>
    /// How far the sound projects in metres.
    /// </summary>
    [DataField]
    public float Range = 3f;

    /// <summary>
    /// The ringtone volume.
    /// </summary>
    [DataField]
    public float Volume = -4f;

    /// <summary>
    /// Whether the ringer is currently playing its ringtone.
    /// </summary>
    [DataField]
    public bool Active;
}
