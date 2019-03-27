using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Sound;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Log;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Sound
{
    public class SoundComponent : SharedSoundComponent
    {
        public void StopAllSounds(INetChannel channel = null)
        {
            SendNetworkMessage(new StopAllSoundsMessage(), channel);
        }

        public void StopSoundSchedule(string filename, INetChannel channel = null)
        {
            SendNetworkMessage(new StopSoundScheduleMessage(){Filename = filename}, channel);
        }

        public void AddScheduledSound(ScheduledSound schedule, INetChannel channel = null)
        {
            SendNetworkMessage(new ScheduledSoundMessage() {Schedule = schedule}, channel);
        }
    }
}
