using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

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

        // Note that except for some rare exceptions, every ambient sound source appears to be static. So:
        // TODO AMBIENT SOUND Use only static queries
        // This would make all the lookups significantly faster. There are some rare exceptions, like flies, vehicles,
        // and the singularity. But those can just play sounds via some other system. Alternatively: give ambient sound
        // its own client-side tree to avoid this issue altogether.
        private static LookupFlags _flags = LookupFlags.Static | LookupFlags.Dynamic | LookupFlags.Sundries;

        private static AudioParams _params = AudioParams.Default.WithVariation(0.01f).WithLoop(true).WithAttenuation(Attenuation.LinearDistance);
            
        /// <summary>
        /// How many times we can be playing 1 particular sound at once.
        /// </summary>
        private int MaxSingleSound => (int) (_maxAmbientCount / (16.0f / 6.0f));

        private readonly Dictionary<AmbientSoundComponent, (IPlayingAudioStream? Stream, string Sound)> _playingSounds = new();
        private readonly Dictionary<string, int> _playingCount = new();

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
            if (!_playingSounds.Remove(component, out var sound))
                return;

            sound.Stream?.Stop();
            _playingCount[sound.Sound] -= 1;
            if (_playingCount[sound.Sound] == 0)
                _playingCount.Remove(sound.Sound);
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
            if (!EntityManager.TryGetComponent(player, out TransformComponent? xform))
            {
                ClearSounds();
                return;
            }

            ProcessNearbyAmbience(xform);
        }

        private void ClearSounds()
        {
            foreach (var (stream, _) in _playingSounds.Values)
            {
                stream?.Stop();
            }

            _playingSounds.Clear();
            _playingCount.Clear();
        }

        private Dictionary<string, List<(float Importance, AmbientSoundComponent)>> GetNearbySources(TransformComponent playerXform, MapCoordinates coords, EntityQuery<TransformComponent> xformQuery)
        {
            var sourceDict = new Dictionary<string, List<(float, AmbientSoundComponent)>>(16);
            var ambientQuery = GetEntityQuery<AmbientSoundComponent>();

            // TODO add variant of GetComponentsInRange that also returns the transform component.
            foreach (var entity in _lookup.GetEntitiesInRange(coords, _maxAmbientRange, flags: _flags))
            {
                if (!ambientQuery.TryGetComponent(entity, out var ambientComp) || !ambientComp.Enabled)
                    continue;

                var xform = xformQuery.GetComponent(entity);
                var delta = xform.ParentUid == playerXform.ParentUid
                    ? xform.LocalPosition - playerXform.LocalPosition
                    : xform.WorldPosition - coords.Position;

                var range = delta.Length;
                if (range >= ambientComp.Range)
                    continue;

                string key;

                if (ambientComp.Sound is SoundPathSpecifier path)
                    key = path.Path?.ToString() ?? string.Empty;
                else
                    key = ((SoundCollectionSpecifier) ambientComp.Sound).Collection ?? string.Empty;

                var list = sourceDict.GetOrNew(key);

                // Prioritize far away & loud sounds.
                list.Add((range * (ambientComp.Volume + 32), ambientComp));
            }

            return sourceDict;
        }

        /// <summary>
        /// Get a list of ambient components in range and determine which ones to start playing.
        /// </summary>
        private void ProcessNearbyAmbience(TransformComponent playerXform)
        {
            var query = GetEntityQuery<TransformComponent>();
            var mapPos = playerXform.MapPosition;

            // Remove out-of-range ambiences
            foreach (var (comp, sound) in _playingSounds)
            {
                var entity = comp.Owner;
                if (comp.Enabled && query.TryGetComponent(entity, out var xform) && xform.MapID == playerXform.MapID)
                {
                    var distance = (xform.ParentUid == playerXform.ParentUid)
                        ? xform.LocalPosition - playerXform.LocalPosition
                        : xform.WorldPosition - mapPos.Position;

                    if (distance.LengthSquared < comp.Range * comp.Range)
                        continue;
                }

                sound.Stream?.Stop();
                _playingSounds.Remove(comp);
                _playingCount[sound.Sound] -= 1;
                if (_playingCount[sound.Sound] == 0)
                    _playingCount.Remove(sound.Sound);
            }

            if (_playingSounds.Count >= _maxAmbientCount)
                return;

            // Add in range ambiences
            foreach (var (key, sources) in GetNearbySources(playerXform, mapPos, query))
            {
                if (_playingSounds.Count >= _maxAmbientCount)
                    break;

                if (_playingCount.TryGetValue(key, out var playingCount) && playingCount >= MaxSingleSound)
                    continue;

                sources.Sort(static (a, b) => b.Importance.CompareTo(a.Importance));

                foreach (var (_, comp) in sources)
                {
                    if (_playingSounds.ContainsKey(comp))
                        continue;

                    var audioParams = _params
                        .AddVolume(comp.Volume + _ambienceVolume)
                        // Randomise start so 2 sources don't increase their volume.
                        .WithPlayOffset(_random.NextFloat(0.0f, 100.0f))
                        .WithMaxDistance(comp.Range);

                    var stream = _audio.PlayPvs(comp.Sound, comp.Owner, audioParams);
                    if (stream == null)
                        continue;

                    _playingSounds[comp] = (stream, key);
                    playingCount++;

                    if (_playingSounds.Count >= _maxAmbientCount)
                        break;
                }

                if (playingCount != 0)
                    _playingCount[key] = playingCount;
            }

            DebugTools.Assert(_playingCount.All(x => x.Value == PlayingCount(x.Key)));
        }
    }
}
