using System.Collections.Generic;
using System.Linq;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Audio
{
    //TODO: This is using a incomplete version of the whole "only play nearest sounds" algo, that breaks down a bit should the ambient sound cap get hit.
    //TODO: This'll be fixed when GetEntitiesInRange produces consistent outputs.

    /// <summary>
    /// Samples nearby <see cref="AmbientSoundComponent"/> and plays audio.
    /// </summary>
    public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private AmbientSoundOverlay? _overlay;
        private int _maxAmbientCount;
        private bool _overlayEnabled;
        private float _maxAmbientRange;
        private float _cooldown;
        private TimeSpan _targetTime = TimeSpan.Zero;
        private float _ambienceVolume = 0.0f;

        /// <summary>
        /// How many times we can be playing 1 particular sound at once.
        /// </summary>
        private int MaxSingleSound => (int) (_maxAmbientCount / (16.0f / 6.0f));

        private readonly Dictionary<AmbientSoundComponent, (IPlayingAudioStream? Stream, string Sound)> _playingSounds = new();

        private const float RangeBuffer = 3f;

        public bool OverlayEnabled
        {
            get => _overlayEnabled;
            set
            {
                if (_overlayEnabled == value) return;
                _overlayEnabled = value;
                var overlayManager = IoCManager.Resolve<IOverlayManager>();

                if (_overlayEnabled)
                {
                    _overlay = new AmbientSoundOverlay(EntityManager, this, EntityManager.System<EntityLookupSystem>());
                    overlayManager.AddOverlay(_overlay);
                }
                else
                {
                    overlayManager.RemoveOverlay(_overlay!);
                    _overlay = null;
                }
            }
        }

        /// <summary>
        /// Is this AmbientSound actively playing right now?
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public bool IsActive(AmbientSoundComponent component)
        {
            return _playingSounds.ContainsKey(component);
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;

            _cfg.OnValueChanged(CCVars.AmbientCooldown, SetCooldown, true);
            _cfg.OnValueChanged(CCVars.MaxAmbientSources, SetAmbientCount, true);
            _cfg.OnValueChanged(CCVars.AmbientRange, SetAmbientRange, true);
            _cfg.OnValueChanged(CCVars.AmbienceVolume, SetAmbienceVolume, true);
            SubscribeLocalEvent<AmbientSoundComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnShutdown(EntityUid uid, AmbientSoundComponent component, ComponentShutdown args)
        {
            if (_playingSounds.Remove(component, out var sound))
                sound.Stream?.Stop();
        }

        private void SetAmbienceVolume(float value) => _ambienceVolume = value;
        private void SetCooldown(float value) => _cooldown = value;
        private void SetAmbientCount(int value) => _maxAmbientCount = value;
        private void SetAmbientRange(float value) => _maxAmbientRange = value;

        public override void Shutdown()
        {
            base.Shutdown();
            ClearSounds();

            _cfg.UnsubValueChanged(CCVars.AmbientCooldown, SetCooldown);
            _cfg.UnsubValueChanged(CCVars.MaxAmbientSources, SetAmbientCount);
            _cfg.UnsubValueChanged(CCVars.AmbientRange, SetAmbientRange);
            _cfg.UnsubValueChanged(CCVars.AmbienceVolume, SetAmbienceVolume);
        }

        private int PlayingCount(string countSound)
        {
            var count = 0;

            foreach (var (_, (_, sound)) in _playingSounds)
            {
                if (sound.Equals(countSound))
                    count++;
            }

            return count;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            if (_cooldown <= 0f)
                return;

            if (_gameTiming.CurTime < _targetTime)
                return;

            _targetTime = _gameTiming.CurTime+TimeSpan.FromSeconds(_cooldown);

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (!EntityManager.TryGetComponent(player, out TransformComponent? playerManager))
            {
                ClearSounds();
                return;
            }

            var coordinates = playerManager.Coordinates;

            ProcessNearbyAmbience(coordinates);
        }

        private void ClearSounds()
        {
            foreach (var (_, (stream, _)) in _playingSounds)
            {
                stream?.Stop();
            }

            _playingSounds.Clear();
        }

        private Dictionary<string, List<AmbientSoundComponent>> GetNearbySources(EntityCoordinates coordinates)
        {
            //TODO: Make this produce a hashset of nearby entities again.
            var sourceDict = new Dictionary<string, List<AmbientSoundComponent>>(16);
            var ambientQuery = GetEntityQuery<AmbientSoundComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var entity in _lookup.GetEntitiesInRange(coordinates, _maxAmbientRange + RangeBuffer))
            {
                if (!ambientQuery.TryGetComponent(entity, out var ambientComp) ||
                    !ambientComp.Enabled ||
                    !xformQuery.GetComponent(entity).Coordinates.TryDistance(EntityManager, coordinates, out var range) ||
                    range > ambientComp.Range)
                {
                    continue;
                }

                var key = _audio.GetSound(ambientComp.Sound);

                if (!sourceDict.ContainsKey(key))
                    sourceDict[key] = new List<AmbientSoundComponent>(MaxSingleSound);

                sourceDict[key].Add(ambientComp);
            }

            // TODO: Just store the distance from above...
            foreach (var (key, val) in sourceDict)
            {
                sourceDict[key] = val.OrderByDescending(x =>
                    Transform(x.Owner).Coordinates.TryDistance(EntityManager, coordinates, out var dist) ? dist * (x.Volume + 32) : float.MaxValue).ToList();
            }

            return sourceDict;
        }

        /// <summary>
        /// Get a list of ambient components in range and determine which ones to start playing.
        /// </summary>
        private void ProcessNearbyAmbience(EntityCoordinates coordinates)
        {
            var compsInRange= GetNearbySources(coordinates);

            var keys = compsInRange.Keys.ToHashSet();

            while (keys.Count != 0)
            {
                if (_playingSounds.Count >= _maxAmbientCount)
                {
                    /*
                    // Go through and remove everything from compSet
                    foreach (var toRemove in keys.SelectMany(key => compsInRange[key]))
                    {
                        compSet.Remove(toRemove.Owner);
                    }
                    */

                    break;
                }

                foreach (var key in keys)
                {
                    if (_playingSounds.Count >= _maxAmbientCount)
                        break;

                    if (compsInRange[key].Count == 0)
                    {
                        keys.Remove(key);
                        continue;
                    }

                    var comp = compsInRange[key].Pop();
                    if (_playingSounds.ContainsKey(comp))
                        continue;

                    var sound = _audio.GetSound(comp.Sound);

                    if (PlayingCount(sound) >= MaxSingleSound)
                    {
                        keys.Remove(key);
                        /*foreach (var toRemove in compsInRange[key])
                        {
                            Logger.Debug($"removing {toRemove.Owner} from set.");
                            compSet.Remove(toRemove.Owner);
                        }*/
                        compsInRange[key].Clear(); // reduce work later should we overrun the max sounds.
                        continue;
                    }

                    var audioParams = AudioParams.Default
                        .WithVariation(0.01f)
                        .WithVolume(comp.Volume + _ambienceVolume)
                        .WithLoop(true)
                        .WithAttenuation(Attenuation.LinearDistance)
                        // Randomise start so 2 sources don't increase their volume.
                        .WithPlayOffset(_random.NextFloat(0.0f, 100.0f))
                        .WithMaxDistance(comp.Range);

                    var stream = _audio.PlayPvs(comp.Sound, comp.Owner, audioParams);

                    if (stream == null) continue;

                    _playingSounds[comp] = (stream, sound);
                }
            }

            foreach (var (comp, sound) in _playingSounds)
            {
                var entity = comp.Owner;
                if (comp.Deleted || // includes entity deletion
                    !comp.Enabled ||
                    !EntityManager.GetComponent<TransformComponent>(entity).Coordinates
                        .TryDistance(EntityManager, coordinates, out var range) ||
                    range > comp.Range)
                {
                    sound.Stream?.Stop();
                    _playingSounds.Remove(comp);
                }
            }

            //TODO: Put this code back in place! Currently not done this way because of GetEntitiesInRange being funny.
            /*
            foreach (var (comp, sound) in _playingSounds)
            {
                if (compSet.Contains(comp.Owner)) continue;

                Logger.Debug($"Cancelled {comp.Owner}");
                _playingSounds[comp].Stream?.Stop();
                _playingSounds.Remove(comp);
            }
            */
        }
    }
}
