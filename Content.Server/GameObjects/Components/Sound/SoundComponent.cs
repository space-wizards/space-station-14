using System;
using System.Collections.Generic;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Serialization;
using SS14.Shared.Timers;

namespace Content.Server.GameObjects.Components.Sound
{
    public enum SoundType
    {
        Normal,
        Global,
        Positional,
    }
    public struct SoundSchedule
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
        public int Delay;

        /// <summary>
        /// Maximum number of milliseconds to add or subtract
        /// from the delay randomly. Useful for random ambience noises.
        /// </summary>
        public int RandomDelay;

        /// <summary>
        /// How many times to repeat the sound. If it's 0, it will play the sound once.
        /// If it's less than 0, it will repeat the sound indefinitely.
        /// If it's greater than 0, it will play the sound n+1 times.
        /// </summary>
        public int Times;

        /// <summary>
        /// How to play the sound.
        /// Normal plays the sound following the entity.
        /// Global plays the sound globally, without position.
        /// Positional plays the sound at a static position.
        /// </summary>
        public SoundType SoundType;

        /// <summary>
        /// If SoundType is Positional, this will be the
        /// position where the sound plays.
        /// </summary>
        public GridCoordinates SoundPosition;
    }

    public class SoundComponent : Component
    {
        public override string Name => "Sound";

        private List<SoundSchedule> _schedules;
        private AudioSystem _audioSystem;

        public void Clear()
        {
            _schedules.Clear();
        }

        public void AddSchedule(SoundSchedule schedule)
        {
            _schedules.Add(schedule);

        }

        public void Play(string filename, AudioParams? audioParams = null)
        {
            _audioSystem.Play(filename, Owner, audioParams);
        }

        public void PlayGlobally(string filename, AudioParams? audioParams = null)
        {
            _audioSystem.Play(filename, audioParams);
        }

        public void PlayPositionally(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            _audioSystem.Play(filename, coordinates, audioParams);
        }

        public override void Initialize()
        {
            base.Initialize();
            _audioSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }
    }
}
