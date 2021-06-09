#nullable enable
using System;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Sound
{
    public class SharedLoopingSoundComponent : Component
    {
        public override string Name => "LoopingSound";
        public override uint? NetID => ContentNetIDs.SOUND;

        /// <summary>
        /// Stops all sounds.
        /// </summary>
        public virtual void StopAllSounds()
        {}

        /// <summary>
        /// Stops a certain scheduled sound from playing.
        /// </summary>
        public virtual void StopScheduledSound(string filename)
        {}

        /// <summary>
        /// Adds an scheduled sound to be played.
        /// </summary>
        public virtual void AddScheduledSound(ScheduledSound scheduledSound)
        {}

        /// <summary>
        ///     Play an audio file following the entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        public void Play(string filename, AudioParams? audioParams = null)
        {
            AddScheduledSound(new ScheduledSound()
            {
                Filename = filename,
                AudioParams = audioParams,
            });
        }
    }

    [NetSerializable, Serializable]
    public class ScheduledSoundMessage : ComponentMessage
    {
        public ScheduledSound Schedule = new();
        public ScheduledSoundMessage()
        {
            Directed = true;
        }
    }

    [NetSerializable, Serializable]
    public class StopSoundScheduleMessage : ComponentMessage
    {
        public string Filename = string.Empty;
        public StopSoundScheduleMessage()
        {
            Directed = true;
        }
    }

    [NetSerializable, Serializable]
    public class StopAllSoundsMessage : ComponentMessage
    {
        public StopAllSoundsMessage()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    [DataDefinition]
    public class ScheduledSound
    {
        [DataField("fileName")]
        public string Filename = string.Empty;

        /// <summary>
        /// The parameters to play the sound with.
        /// </summary>
        [DataField("audioparams")]
        public AudioParams? AudioParams;

        /// <summary>
        /// Delay in milliseconds before playing the sound,
        /// and delay between repetitions if Times is not 0.
        /// </summary>
        [DataField("delay")]
        public uint Delay;

        /// <summary>
        /// Maximum number of milliseconds to add to the delay randomly.
        /// Useful for random ambience noises. Generated value differs from client to client.
        /// </summary>
        [DataField("randomdelay")]
        public uint RandomDelay;

        /// <summary>
        /// How many times to repeat the sound. If it's 0, it will play the sound once.
        /// If it's less than 0, it will repeat the sound indefinitely.
        /// If it's greater than 0, it will play the sound n+1 times.
        /// </summary>
        [DataField("times")]
        public int Times;

        /// <summary>
        /// Whether the sound will play or not.
        /// </summary>
        public bool Play = true;
    }
}
