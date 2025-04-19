using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.PDA.Ringer;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRingerSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class RingerComponent : Component
{
    /// <summary>
    /// The ringtone, represented as an array of notes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Note[] Ringtone = new Note[SharedRingerSystem.RingtoneLength];

    /// <summary>
    /// The last time this ringer's ringtone was set.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextRingtoneSetTime;

    /// <summary>
    /// The time when the next note should play.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan? NextNoteTime;

    /// <summary>
    /// The cooldown before the ringtone can be changed again.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Keeps track of how many notes have elapsed if the ringer component is playing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int NoteCount;

    /// <summary>
    /// How far the sound projects in metres.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 3f;

    /// <summary>
    /// The ringtone volume.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Volume = -4f;

    /// <summary>
    /// Whether the ringer is currently playing its ringtone.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;
}
