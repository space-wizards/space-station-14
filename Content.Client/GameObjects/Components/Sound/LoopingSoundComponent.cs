using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.Physics;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;

namespace Content.Client.GameObjects.Components.Sound
{
    [RegisterComponent]
    public class LoopingSoundComponent : SharedLoopingSoundComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly Dictionary<ScheduledSound, IPlayingAudioStream> _audioStreams = new();
        private AudioSystem _audioSystem;

        public override void StopAllSounds()
        {
            foreach (var kvp in _audioStreams)
            {
                kvp.Key.Play = false;
                kvp.Value.Stop();
            }
            _audioStreams.Clear();
        }

        public override void StopScheduledSound(string filename)
        {
            foreach (var kvp in _audioStreams)
            {
                if (kvp.Key.Filename != filename) continue;
                kvp.Key.Play = false;
                kvp.Value.Stop();
                _audioStreams.Remove(kvp.Key);
            }
        }

        public override void AddScheduledSound(ScheduledSound schedule)
        {
            Play(schedule);
        }

        public void Play(ScheduledSound schedule)
        {
            if (!schedule.Play) return;

            Owner.SpawnTimer((int) schedule.Delay + (_random.Next((int) schedule.RandomDelay)),() =>
                {
                    if (!schedule.Play) return; // We make sure this hasn't changed.
                    if (_audioSystem == null) _audioSystem = EntitySystem.Get<AudioSystem>();

                    if (!_audioStreams.ContainsKey(schedule))
                    {
                        _audioStreams.Add(schedule,_audioSystem.Play(schedule.Filename, Owner, schedule.AudioParams));
                    }
                    else
                    {
                        _audioStreams[schedule] = _audioSystem.Play(schedule.Filename, Owner, schedule.AudioParams);
                    }

                    if (schedule.Times == 0) return;

                    if (schedule.Times > 0) schedule.Times--;

                    Play(schedule);
                });
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);
            switch (message)
            {
                case ScheduledSoundMessage msg:
                    AddScheduledSound(msg.Schedule);
                    break;

                case StopSoundScheduleMessage msg:
                    StopScheduledSound(msg.Filename);
                    break;

                case StopAllSoundsMessage _:
                    StopAllSounds();
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (EntitySystem.TryGet(out _audioSystem)) _audioSystem.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadFunction(
                "schedules",
                new List<ScheduledSound>(),
                schedules => schedules.ForEach(AddScheduledSound));
        }
    }
}
