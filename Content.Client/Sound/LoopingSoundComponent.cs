using System.Collections.Generic;
using Content.Shared.Physics;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Sound
{
    [RegisterComponent]
    public class LoopingSoundComponent : SharedLoopingSoundComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly Dictionary<ScheduledSound, IPlayingAudioStream> _audioStreams = new();

        [DataField("schedules", true)]
        private List<ScheduledSound> _scheduledSounds
        {
            set => value.ForEach(AddScheduledSound);
            get => new();
        }

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

                    if (!_audioStreams.ContainsKey(schedule))
                    {
                        _audioStreams.Add(schedule, SoundSystem.Play(Filter.Local(), schedule.Filename, Owner, schedule.AudioParams)!);
                    }
                    else
                    {
                        _audioStreams[schedule] = SoundSystem.Play(Filter.Local(), schedule.Filename, Owner, schedule.AudioParams)!;
                    }

                    if (schedule.Times == 0) return;

                    if (schedule.Times > 0) schedule.Times--;

                    Play(schedule);
                });
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
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

        protected override void Initialize()
        {
            base.Initialize();
            SoundSystem.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
        }
    }
}
