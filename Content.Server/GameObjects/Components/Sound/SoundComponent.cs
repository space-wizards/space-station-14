using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Sound;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Sound
{
    public class SoundComponent : SharedSoundComponent
    {
        public void StopAllSounds(INetChannel channel)
        {
            SendNetworkMessage(new StopAllSoundsMessage(), channel);
        }

        public void StopScheduledSound(string filename, INetChannel channel)
        {
            SendNetworkMessage(new StopSoundScheduleMessage(){Filename = filename}, channel);
        }

        public void AddScheduledSound(ScheduledSound schedule, INetChannel channel)
        {
            SendNetworkMessage(new ScheduledSoundMessage() {Schedule = schedule}, channel);
        }

        public override void StopAllSounds()
        {
            StopAllSounds(null);
        }

        public override void StopScheduledSound(string filename)
        {
            StopScheduledSound(filename, null);
        }

        public override void AddScheduledSound(ScheduledSound schedule)
        {
            AddScheduledSound(schedule, null);
        }

        public void Play(string filename, AudioParams? audioParams = null, INetChannel channel = null)
        {
            AddScheduledSound(new ScheduledSound()
            {
                Filename = filename,
                AudioParams = audioParams,
                SoundType = SoundType.Global
            }, channel);
        }

        public void Play(string filename, IEntity entity, AudioParams? audioParams = null, INetChannel channel = null)
        {
            AddScheduledSound(new ScheduledSound()
            {
                Filename = filename,
                AudioParams = audioParams,
                SoundType = SoundType.Normal,
                Entity = entity
            }, channel);
        }

        public void Play(string filename, GridCoordinates coordinates, AudioParams? audioParams = null, INetChannel channel = null)
        {
            AddScheduledSound(new ScheduledSound()
            {
                Filename = filename,
                AudioParams = audioParams,
                SoundType = SoundType.Positional,
                SoundPosition = coordinates
            }, channel);
        }
    }
}
