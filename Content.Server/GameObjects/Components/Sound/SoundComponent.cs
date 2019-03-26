using Content.Shared.GameObjects.Components.Sound;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Log;

namespace Content.Server.GameObjects.Components.Sound
{
    public class SoundComponent : SharedSoundComponent
    {
        private IEntityNetworkManager _networkManager;
        public void StopAllSounds(INetChannel channel = null)
        {
            SendNetworkMessage(new StopAllSoundsMessage(), channel);
        }

        public void StopSoundSchedule(string filename, INetChannel channel = null)
        {
            SendNetworkMessage(new StopSoundScheduleMessage(){Filename = filename}, channel);
        }

        public void AddSoundSchedule(ScheduledSound schedule, INetChannel channel = null)
        {
            SendNetworkMessage(new ScheduledSoundMessage() { Schedule = schedule}, channel);
        }

        public override void Initialize()
        {
            base.Initialize();

        }
    }
}
