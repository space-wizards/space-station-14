using Content.Shared.GameObjects.Components.Sound;
using SS14.Shared.Interfaces.Network;

namespace Content.Server.GameObjects.Sound
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

        public void AddSoundSchedule(SoundSchedule schedule, INetChannel channel = null)
        {
            SendNetworkMessage(new SoundScheduleMessage() { Schedule = schedule}, channel);
        }
    }
}
