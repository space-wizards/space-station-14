using Content.Shared.PDA;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA.Ringer
{
    [RegisterComponent, ComponentProtoName("Ringer")]
    public sealed class RingerComponent : Component
    {
        [ViewVariables]
        [DataField("ringtone")]
        public Note[] Ringtone = new Note[SharedRingerSystem.RingtoneLength];

        [DataField("timeElapsed")]
        public float TimeElapsed = 0;

        /// <summary>
        /// Keeps track of how many notes have elapsed if the ringer component is playing.
        /// </summary>
        [DataField("noteCount")]
        public int NoteCount = 0;

        [DataField("isPlaying")]
        public bool IsPlaying = false;
    }
}
