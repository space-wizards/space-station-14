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
using SS14.Shared.Serialization;
using SS14.Shared.Timers;

namespace Content.Client.GameObjects.Components.Sound
{
    public class SoundComponent : SharedSoundComponent
    {
        private readonly List<ScheduledSound> _schedules = new List<ScheduledSound>();
        private AudioSystem _audioSystem;
        private Random Random;

        public override void StopAllSounds()
        {
            foreach (var schedule in _schedules)
            {
                schedule.Play = false;
            }
            _schedules.Clear();
        }

        public override void StopScheduledSound(string filename)
        {
            foreach (var schedule in _schedules.ToArray())
            {
                if (schedule.Filename != filename) continue;
                schedule.Play = false;
                _schedules.Remove(schedule);
            }
        }

        public override void AddScheduledSound(ScheduledSound schedule)
        {
            _schedules.Add(schedule);
            Play(schedule);
        }

        public void Play(ScheduledSound schedule)
        {
            if (!schedule.Play) return;
            if (Random == null) Random = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());

            Timer.Spawn((int) schedule.Delay + (Random.Next((int) schedule.RandomDelay)),() =>
                {
                    if (!schedule.Play) return; // We make sure this hasn't changed.
                    if (_audioSystem == null) _audioSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
                    _audioSystem.Play(schedule.Filename, Owner, schedule.AudioParams);

                    if (schedule.Times == 0)
                    {
                        _schedules.Remove(schedule);
                        return;
                    }

                    if (schedule.Times > 0)
                        schedule.Times--;

                    Play(schedule);
                });
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case ScheduledSoundMessage msg:
                    AddScheduledSound(msg.Schedule);
                    break;

                case StopSoundScheduleMessage msg:
                    StopScheduledSound(msg.Filename);
                    break;

                case StopAllSoundsMessage msg:
                    StopAllSounds();
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem(out _audioSystem);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if (serializer.Writing) return;
            serializer.TryReadDataField("schedules", out List<ScheduledSound> schedules);
            if (schedules == null) return;
            foreach (var schedule in schedules)
            {
                if (schedule == null) continue;
                AddScheduledSound(schedule);
            }
        }
    }
}
