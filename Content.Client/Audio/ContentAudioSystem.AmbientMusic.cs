using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Random;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly RulesSystem _rules = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly TimeSpan _minAmbienceTime = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _maxAmbienceTime = TimeSpan.FromSeconds(60);

    private const float AmbientMusicFadeTime = 10f;
    private static float _volumeSlider;

    // Don't need to worry about this being serializable or pauseable as it doesn't affect the sim.
    private TimeSpan _nextAudio;

    private EntityUid? _ambientMusicStream;
    private AmbientMusicPrototype? _musicProto;

    /// <summary>
    /// If we find a better ambient music proto can we interrupt this one.
    /// </summary>
    private bool _interruptable;

    /// <summary>
    /// Track what ambient sounds we've played. This is so they all get played an even
    /// number of times.
    /// When we get to the end of the list we'll re-shuffle
    /// </summary>
    private readonly Dictionary<string, List<ResPath>> _ambientSounds = new();

    private ISawmill _sawmill = default!;

    private void InitializeAmbientMusic()
    {
        _configManager.OnValueChanged(CCVars.AmbientMusicVolume, AmbienceCVarChanged, true);
        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("audio.ambience");

        // Reset audio
        _nextAudio = TimeSpan.MaxValue;

        SetupAmbientSounds();
        _proto.PrototypesReloaded += OnProtoReload;
        _state.OnStateChanged += OnStateChange;
        // On round end summary OR lobby cut audio.
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEndMessage);
    }

    private void AmbienceCVarChanged(float obj)
    {
        _volumeSlider = obj;

        if (_ambientMusicStream != null && _musicProto != null)
        {
            _audio.SetVolume(_ambientMusicStream, _musicProto.Sound.Params.Volume + _volumeSlider);
        }
    }

    private void ShutdownAmbientMusic()
    {
        _configManager.UnsubValueChanged(CCVars.AmbientMusicVolume, AmbienceCVarChanged);
        _proto.PrototypesReloaded -= OnProtoReload;
        _state.OnStateChanged -= OnStateChange;
        _ambientMusicStream = _audio.Stop(_ambientMusicStream);
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(AmbientMusicPrototype)) &&
            !obj.ByType.ContainsKey(typeof(RulesPrototype)))
        {
            return;
        }

        _ambientSounds.Clear();
        SetupAmbientSounds();
    }

    private void OnStateChange(StateChangedEventArgs obj)
    {
        if (obj.NewState is not GameplayState)
            return;

        // If they go to game then reset the ambience timer.
        _nextAudio = _timing.CurTime + _random.Next(_minAmbienceTime, _maxAmbienceTime);
    }

    private void SetupAmbientSounds()
    {
        foreach (var ambience in _proto.EnumeratePrototypes<AmbientMusicPrototype>())
        {
            var tracks = _ambientSounds.GetOrNew(ambience.ID);
            RefreshTracks(ambience.Sound, tracks, null);
            _random.Shuffle(tracks);
        }
    }

    private void OnRoundEndMessage(RoundEndMessageEvent ev)
    {
        // If scoreboard shows then just stop the music
        _ambientMusicStream = _audio.Stop(_ambientMusicStream);
        _nextAudio = TimeSpan.FromMinutes(3);
    }

    private void RefreshTracks(SoundSpecifier sound, List<ResPath> tracks, ResPath? lastPlayed)
    {
        DebugTools.Assert(tracks.Count == 0);

        switch (sound)
        {
            case SoundCollectionSpecifier collection:
                if (collection.Collection == null)
                    break;

                var slothCud = _proto.Index<SoundCollectionPrototype>(collection.Collection);
                tracks.AddRange(slothCud.PickFiles);
                break;
            case SoundPathSpecifier path:
                tracks.Add(path.Path);
                break;
        }

        // Just so the same track doesn't play twice
        if (tracks.Count > 1 && tracks[^1] == lastPlayed)
        {
            (tracks[0], tracks[^1]) = (tracks[^1], tracks[0]);
        }
    }

    private void UpdateAmbientMusic()
    {
        // Update still runs in lobby so just ignore it.
        if (_state.CurrentState is not GameplayState)
        {
            FadeOut(_ambientMusicStream);
            _ambientMusicStream = null;
            _musicProto = null;
            return;
        }

        bool? isDone = null;

        if (TryComp(_ambientMusicStream, out AudioComponent? audioComp))
        {
            isDone = !audioComp.Playing;
        }

        if (_interruptable)
        {
            var player = _player.LocalSession?.AttachedEntity;

            if (player == null || _musicProto == null || !_rules.IsTrue(player.Value, _proto.Index<RulesPrototype>(_musicProto.Rules)))
            {
                FadeOut(_ambientMusicStream, duration: AmbientMusicFadeTime);
                _musicProto = null;
                _interruptable = false;
                isDone = true;
            }
        }

        // Still running existing ambience
        if (isDone == false)
            return;

        // If ambience finished reset the CD (this also means if we have long ambience it won't clip)
        if (isDone == true)
        {
            // Also don't need to worry about rounding here as it doesn't affect the sim
            _nextAudio = _timing.CurTime + _random.Next(_minAmbienceTime, _maxAmbienceTime);
        }

        _ambientMusicStream = null;

        if (_nextAudio > _timing.CurTime)
            return;

        _musicProto = GetAmbience();

        if (_musicProto == null)
        {
            _interruptable = false;
            return;
        }

        _interruptable = _musicProto.Interruptable;
        var tracks = _ambientSounds[_musicProto.ID];

        var track = tracks[^1];
        tracks.RemoveAt(tracks.Count - 1);

        var strim = _audio.PlayGlobal(
            track.ToString(),
            Filter.Local(),
            false,
            AudioParams.Default.WithVolume(_musicProto.Sound.Params.Volume + _volumeSlider));

        _ambientMusicStream = strim.Value.Entity;

        if (_musicProto.FadeIn)
        {
            FadeIn(_ambientMusicStream, strim.Value.Component, AmbientMusicFadeTime);
        }

        // Refresh the list
        if (tracks.Count == 0)
        {
            RefreshTracks(_musicProto.Sound, tracks, track);
        }
    }

    private AmbientMusicPrototype? GetAmbience()
    {
        var player = _player.LocalPlayer?.ControlledEntity;

        if (player == null)
            return null;

        var ev = new PlayAmbientMusicEvent();
        RaiseLocalEvent(ref ev);

        if (ev.Cancelled)
            return null;

        var ambiences = _proto.EnumeratePrototypes<AmbientMusicPrototype>().ToList();
        ambiences.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var amb in ambiences)
        {
            if (!_rules.IsTrue(player.Value, _proto.Index<RulesPrototype>(amb.Rules)))
                continue;

            return amb;
        }

        _sawmill.Warning($"Unable to find fallback ambience track");
        return null;
    }

    /// <summary>
    /// Fades out the current ambient music temporarily.
    /// </summary>
    public void DisableAmbientMusic()
    {
        FadeOut(_ambientMusicStream);
        _ambientMusicStream = null;
    }
}
