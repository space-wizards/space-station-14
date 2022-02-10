using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.PDA.Ringer
{
    [Serializable, NetSerializable]
    public sealed class RingerUpdateState : BoundUserInterfaceState
    {
        public bool IsPlaying;
        public Note[] Ringtone;

        public RingerUpdateState(bool isPlay, Note[] ringtone)
        {
            IsPlaying = isPlay;
            Ringtone = ringtone;
        }
    }

}
