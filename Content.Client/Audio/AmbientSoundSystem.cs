using System.Collections.Generic;
using System.Linq;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Audio
{
    /// <summary>
    /// Samples nearby <see cref="AmbientSoundComponent"/> and plays audio.
    /// </summary>
    public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
    {
        [Dependency] private IEntityLookup _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private int _maxAmbientCount;

        private float _maxAmbientRange;
        private float _cooldown;
        private float _accumulator;

        /// <summary>
        /// How many times we can be playing 1 particular sound at once.
        /// </summary>
        private int _maxSingleSound = 3;

        private Dictionary<AmbientSoundComponent, (IPlayingAudioStream? Stream, string Sound)> _playingSounds = new();

        private const float RangeBuffer = 0.5f;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.AmbientCooldown, SetCooldown, true);
            configManager.OnValueChanged(CCVars.MaxAmbientSources, SetAmbientCount, true);
            configManager.OnValueChanged(CCVars.AmbientRange, SetAmbientRange, true);
        }

        private void SetCooldown(float value) => _cooldown = value;
        private void SetAmbientCount(int value) => _maxAmbientCount = value;
        private void SetAmbientRange(float value) => _maxAmbientRange = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CCVars.AmbientCooldown, SetCooldown);
            configManager.UnsubValueChanged(CCVars.MaxAmbientSources, SetAmbientCount);
            configManager.UnsubValueChanged(CCVars.AmbientRange, SetAmbientRange);
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

            if (!_gameTiming.IsFirstTimePredicted) return;

            if (_cooldown <= 0f)
            {
                _accumulator = 0f;
                return;
            }

            _accumulator += frameTime;
            if (_accumulator < _cooldown) return;
            _accumulator -= _cooldown;

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (!EntityManager.TryGetComponent(player, out TransformComponent? playerManager))
            {
                ClearSounds();
                return;
            }

            var coordinates = playerManager.Coordinates;

            foreach (var (comp, (stream, _)) in _playingSounds.ToArray())
            {
                if (!comp.Deleted && comp.Enabled && EntityManager.GetComponent<TransformComponent>(comp.Owner).Coordinates.TryDistance(EntityManager, coordinates, out var range) &&
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
                if (!EntityManager.TryGetComponent(entity, out AmbientSoundComponent? ambientComp) ||
                    _playingSounds.ContainsKey(ambientComp) ||
                    !ambientComp.Enabled ||
                    // We'll also do this crude distance check because it's what we're doing in the active loop above.
                    !EntityManager.GetComponent<TransformComponent>(entity).Coordinates.TryDistance(EntityManager, coordinates, out var range) ||
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
                    .WithAttenuation(Attenuation.LinearDistance)
                    // Randomise start so 2 sources don't increase their volume.
                    .WithPlayOffset(_random.NextFloat())
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
