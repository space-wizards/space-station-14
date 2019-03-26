using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Sound;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Timers;

namespace Content.Client.GameObjects.Components.Sound
{
    public class SoundComponent : SharedSoundComponent
    {
        private List<SoundSchedule> _schedules = new List<SoundSchedule>();
        private ITimerManager _timerManager;
        private AudioSystem _audioSystem;
        public Random Random;

        public void StopAllSounds()
        {
            foreach (var schedule in _schedules)
            {
                schedule.Play = false;
            }
            _schedules.Clear();
        }

        public void StopSoundSchedule(string filename)
        {
            foreach (var schedule in _schedules)
            {
                if (schedule.Filename != filename) continue;
                schedule.Play = false;
                _schedules.Remove(schedule);
            }
        }

        public void AddSoundSchedule(SoundSchedule schedule)
        {
            _schedules.Add(schedule);
            Play(schedule);
        }

        public void Play(SoundSchedule schedule)
        {
            if (!schedule.Play) return;

            Timer.Delay((int) schedule.Delay + Random.Next((int) schedule.RandomDelay))
                .ContinueWith((task) =>
                {
                    if (!schedule.Play) return; // We make sure this hasn't changed.
                    if (_audioSystem == null) IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem(out _audioSystem);
                    switch (schedule.SoundType)
                    {
                        case SoundType.Normal:
                            _audioSystem?.Play(schedule.Filename, Owner, schedule.AudioParams);
                            break;
                        case SoundType.Global:
                            _audioSystem?.Play(schedule.Filename, schedule.AudioParams);
                            break;
                        case SoundType.Positional:
                            _audioSystem?.Play(schedule.Filename, schedule.SoundPosition, schedule.AudioParams);
                            break;
                    }
                });

            if (schedule.Times == 0)
            {
                _schedules.Remove(schedule);
                return;
            }

            if (schedule.Times > 0)
                schedule.Times--;

            Play(schedule);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case SoundScheduleMessage msg:
                    AddSoundSchedule(msg.Schedule);
                    break;

                case StopSoundScheduleMessage msg:
                    StopSoundSchedule(msg.Filename);
                    break;

                case StopAllSoundsMessage msg:
                    StopAllSounds();
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            Random = new Random();
            _timerManager = IoCManager.Resolve<ITimerManager>();
            IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem(out _audioSystem);
        }
    }
}
