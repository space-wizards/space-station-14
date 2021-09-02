using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Audio
{
    /// <summary>
    /// Samples nearby <see cref="AmbientSoundComponent"/> and plays audio.
    /// </summary>
    public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
    {
        [Dependency] private IEntityLookup _lookup = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private int _maxAmbientCount = 6;

        private float _maxAmbientRange = 5f;

        private const float Cooldown = 0.5f;
        private float _accumulator;

        /// <summary>
        /// How many times we can be playing 1 particular sound at once.
        /// </summary>
        private int _maxSingleSound = 3;

        private Dictionary<AmbientSoundComponent, (IPlayingAudioStream? Stream, string Sound)> _playingSounds = new();

        private const float RangeBuffer = 0.1f;

        public override void Initialize()
        {
            base.Initialize();
        }

        private int PlayingCount(string countSound)
        {
            var count = 0;

            foreach (var (_, (_, sound)) in _playingSounds)
            {
                if (sound.Equals(countSound)) count++;
            }

            return count;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _accumulator += frameTime;
            if (_accumulator < Cooldown) return;
            _accumulator -= Cooldown;

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null)
            {
                ClearSounds();
                return;
            }

            var coordinates = player.Transform.Coordinates;

            foreach (var (comp, (stream, _)) in _playingSounds.ToArray())
            {
                if (!comp.Deleted && comp.Enabled && comp.Owner.Transform.Coordinates.TryDistance(EntityManager, coordinates, out var range) &&
                    range <= comp.Range)
                {
                    continue;
                }

                stream?.Stop();

                _playingSounds.Remove(comp);
            }

            if (_playingSounds.Count >= _maxAmbientCount) return;

            SampleNearby(coordinates);
        }

        private void ClearSounds()
        {
            foreach (var (_, (stream, _)) in _playingSounds)
            {
                stream?.Stop();
            }

            _playingSounds.Clear();
        }

        /// <summary>
        /// Get a list of ambient components in range and determine which ones to start playing.
        /// </summary>
        private void SampleNearby(EntityCoordinates coordinates)
        {
            var compsInRange = new List<AmbientSoundComponent>();

            foreach (var entity in _lookup.GetEntitiesInRange(coordinates, _maxAmbientRange,
                LookupFlags.Approximate | LookupFlags.IncludeAnchored))
            {
                if (!entity.TryGetComponent(out AmbientSoundComponent? ambientComp) ||
                    _playingSounds.ContainsKey(ambientComp) ||
                    !ambientComp.Enabled ||
                    // We'll also do this crude distance check because it's what we're doing in the active loop above.
                    !entity.Transform.Coordinates.TryDistance(EntityManager, coordinates, out var range) ||
                    range > ambientComp.Range - RangeBuffer)
                {
                    continue;
                }

                compsInRange.Add(ambientComp);
            }

            while (_playingSounds.Count < _maxAmbientCount)
            {
                if (compsInRange.Count == 0) break;

                var comp = _random.PickAndTake(compsInRange);
                var sound = comp.Sound.GetSound();

                if (PlayingCount(sound) >= _maxSingleSound) continue;

                var audioParams = AudioHelpers
                    .WithVariation(0.01f)
                    .WithVolume(comp.Volume)
                    .WithLoop(true)
                    .WithMaxDistance(comp.Range);

                var stream = SoundSystem.Play(
                    Filter.Local(),
                    sound,
                    comp.Owner,
                    audioParams);

                if (stream == null) continue;

                _playingSounds[comp] = (stream, sound);
            }
        }
    }
}
