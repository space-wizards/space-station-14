using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Serialization;
using SS14.Shared.Timers;

namespace Content.Shared.GameObjects.Components.Sound
{
    public class SharedSoundComponent : Component
    {
        public override string Name => "Sound";
        public override uint? NetID => ContentNetIDs.SOUND;

        public override bool NetworkSynchronizeExistence => true;
    }

    [NetSerializable, Serializable]
    public class SoundScheduleMessage : ComponentMessage
    {
        public SoundSchedule Schedule;
        public SoundScheduleMessage()
        {
            Directed = true;
        }
    }

    [NetSerializable, Serializable]
    public class StopSoundScheduleMessage : ComponentMessage
    {
        public string Filename;
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

    public enum SoundType
    {
        /// <summary>
        /// Sound follows the entity.
        /// </summary>
        Normal,
        /// <summary>
        /// Sound is played without position.
        /// </summary>
        Global,
        /// <summary>
        /// Sound is static in a given position.
        /// </summary>
        Positional,
    }

    [Serializable, NetSerializable]
    public class SoundSchedule
    {
        public string Filename;

        /// <summary>
        /// The parameters to play the sound with.
        /// </summary>
        public AudioParams? AudioParams;

        /// <summary>
        /// Delay in milliseconds before playing the sound,
        /// and delay between repetitions if Times is not 0.
        /// </summary>
        public uint Delay = 0;

        /// <summary>
        /// Maximum number of milliseconds to add or subtract
        /// from the delay randomly. Useful for random ambience noises.
        /// Generated value might differ from client to client.
        /// </summary>
        public uint RandomDelay = 0;

        /// <summary>
        /// How many times to repeat the sound. If it's 0, it will play the sound once.
        /// If it's less than 0, it will repeat the sound indefinitely.
        /// If it's greater than 0, it will play the sound n+1 times.
        /// </summary>
        public int Times = 0;

        /// <summary>
        /// How to play the sound.
        /// Normal plays the sound following the entity.
        /// Global plays the sound globally, without position.
        /// Positional plays the sound at a static position.
        /// </summary>
        public SoundType SoundType = SoundType.Normal;

        /// <summary>
        /// If SoundType is Positional, this will be the
        /// position where the sound plays.
        /// </summary>
        public GridCoordinates SoundPosition;

        /// <summary>
        /// Whether the sound will play or not.
        /// </summary>
        public bool Play = true;
    }
}
