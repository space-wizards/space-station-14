using Content.Shared.GameObjects.Components.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Server.GameObjects.Components.Sound
{
    [RegisterComponent]
    public class LoopingLoopingSoundComponent : SharedLoopingSoundComponent
    {
        /// <summary>
        /// Stops all sounds.
        /// </summary>
        /// <param name="channel">User that will be affected.</param>
        public void StopAllSounds(INetChannel channel)
        {
            SendNetworkMessage(new StopAllSoundsMessage(), channel);
        }

        /// <summary>
        /// Stops a certain scheduled sound from playing.
        /// </summary>
        /// <param name="channel">User that will be affected.</param>
        public void StopScheduledSound(string filename, INetChannel channel)
        {
            SendNetworkMessage(new StopSoundScheduleMessage(){Filename = filename}, channel);
        }

        /// <summary>
        /// Adds an scheduled sound to be played.
        /// </summary>
        /// <param name="channel">User that will be affected.</param>
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

        /// <summary>
        ///     Play an audio file following the entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="channel">User that will be affected.</param>
        public void Play(string filename, AudioParams? audioParams = null, INetChannel channel = null)
        {
            AddScheduledSound(new ScheduledSound()
            {
                Filename = filename,
                AudioParams = audioParams,
            }, channel);
        }
    }
}
