using Content.Shared.PDA;

namespace Content.Server.PDA.Ringer
{
    [RegisterComponent]
    public sealed partial class RingerComponent : Component
    {
        [DataField("ringtone")]
        public Note[] Ringtone = new Note[SharedRingerSystem.RingtoneLength];

        [DataField]
        public float TimeElapsed = 0;

        /// <summary>
        /// Keeps track of how many notes have elapsed if the ringer component is playing.
        /// </summary>
        [DataField]
        public int NoteCount = 0;

        /// <summary>
        /// How far the sound projects in metres.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Range = 3f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Volume = -4f;
    }

    [RegisterComponent]
    public sealed partial class ActiveRingerComponent : Component
    {
    }
}
