using Content.Shared.PDA;

namespace Content.Server.PDA.Ringer
{
    [RegisterComponent]
    public sealed class RingerComponent : Component
    {
        [DataField("ringtone")]
        public Note[] Ringtone = new Note[SharedRingerSystem.RingtoneLength];

        [DataField("timeElapsed")]
        public float TimeElapsed = 0;

        /// <summary>
        /// Keeps track of how many notes have elapsed if the ringer component is playing.
        /// </summary>
        [DataField("noteCount")]
        public int NoteCount = 0;

        /// <summary>
        /// How far the sound projects in metres.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("range")]
        public float Range = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("volume")]
        public float Volume = -4f;
    }

    [RegisterComponent]
    public sealed class ActiveRingerComponent : Component
    {
    }
}
