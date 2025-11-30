using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.MusicPlayer;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
 

namespace Content.Client.MusicPlayer;

public sealed class MusicPlayerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private MusicPlayerWindow? _window;

    private readonly List<ProtoId<MusicCategoryPrototype>> _categoryOrder = new();
    private readonly Dictionary<ProtoId<MusicCategoryPrototype>, List<ProtoId<MusicTrackPrototype>>> _tracksByCat = new();

    private ProtoId<MusicCategoryPrototype>? _currentCategory;
    private ProtoId<MusicTrackPrototype>? _currentTrack;
    private EntityUid? _currentAudio;
    private float _volume = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<Content.Shared.MusicPlayer.OpenMusicPlayerEvent>(OnOpenMusicPlayer);
    }

    private void OnOpenMusicPlayer(Content.Shared.MusicPlayer.OpenMusicPlayerEvent ev, EntitySessionEventArgs args)
    {
        OpenWindow();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_currentAudio == null)
            return;

        if (!EntityManager.EntityExists(_currentAudio.Value))
        {
            _currentAudio = null;
            Next();
            return;
        }

        if (EntityManager.TryGetComponent(_currentAudio.Value, out AudioComponent? comp))
        {
            // If it's stopped (finished) and not paused, go next.
            if (comp.State == AudioState.Stopped)
            {
                _currentAudio = null;
                Next();
            }
        }
    }

    public void OpenWindow()
    {
        if (_window != null && _window.IsOpen)
        {
            _window.OpenCentered();
            return;
        }

        BuildIndex();

        _window = _ui.CreateWindow<MusicPlayerWindow>();
        _window.OnCategorySelected += SelectCategory;
        _window.OnTrackSelected += SelectTrack;
        _window.OnPlayPause += TogglePlayPause;
        _window.OnStop += Stop;
        _window.OnNext += Next;
        _window.OnSeek += Seek;
        _window.OnVolumeChanged += SetVolume;

        var cats = _categoryOrder
            .Select(id => (_proto.Index(id).Name, id));
        _window.SetCategories(cats);

        if (_categoryOrder.Count > 0)
            SelectCategory(_categoryOrder[0]);

        // Show the window now that it is configured
        _window.OpenCentered();
    }

    private void BuildIndex()
    {
        _categoryOrder.Clear();
        _tracksByCat.Clear();

        foreach (var cat in _proto.EnumeratePrototypes<MusicCategoryPrototype>().OrderBy(c => c.Order))
        {
            _categoryOrder.Add(cat.ID);
            _tracksByCat[cat.ID] = new();
        }

        foreach (var track in _proto.EnumeratePrototypes<MusicTrackPrototype>())
        {
            if (!_tracksByCat.TryGetValue(track.Category, out var list))
            {
                _tracksByCat[track.Category] = list = new();
                if (!_categoryOrder.Contains(track.Category))
                    _categoryOrder.Add(track.Category);
            }
            list.Add(track.ID);
        }

        foreach (var key in _tracksByCat.Keys.ToList())
            _tracksByCat[key] = _tracksByCat[key]
                .Select(id => (id, _proto.Index(id).Name))
                .OrderBy(p => p.Name)
                .Select(p => p.id)
                .ToList();
    }

    private void SelectCategory(ProtoId<MusicCategoryPrototype> cat)
    {
        _currentCategory = cat;
        var tracks = _tracksByCat.GetValueOrDefault(cat, new());
        var items = tracks.Select(id => (_proto.Index(id).Name, id));
        _window?.SetTracks(items);
    }

    private void SelectTrack(ProtoId<MusicTrackPrototype> track)
    {
        _currentTrack = track;
        Play(track);
    }

    private void TogglePlayPause()
    {
        if (_currentAudio == null)
        {
            if (_currentTrack != null)
                Play(_currentTrack.Value);
            return;
        }

        if (_audio.IsPlaying(_currentAudio))
            _audio.SetState(_currentAudio, AudioState.Paused);
        else
            _audio.SetState(_currentAudio, AudioState.Playing);
    }

    private void Stop()
    {
        _audio.Stop(_currentAudio);
        _currentAudio = null;
    }

    private void Seek(float pos)
    {
        var comp = GetAudioComp();
        if (comp != null)
            comp.PlaybackPosition = pos;
    }

    private void SetVolume(float value)
    {
        _volume = Math.Clamp(value, 0f, 1f);
        var comp = GetAudioComp();
        if (comp != null)
        {
            // Convert 0..1 to gain volume scale using SharedAudioSystem.VolumeToGain or set directly to Params.Volume (dB)
            // Here we set source volume linearly via Params.Volume in dB: use helper VolumeToGain? Instead, use SetGain for simplicity.
            EntitySystem.Get<Robust.Shared.Audio.Systems.SharedAudioSystem>().SetGain(_currentAudio, _volume, comp);
        }
    }

    private AudioComponent? GetAudioComp()
    {
        if (_currentAudio != null && EntityManager.TryGetComponent(_currentAudio.Value, out AudioComponent? comp))
            return comp;
        return null;
    }

    private void Play(ProtoId<MusicTrackPrototype> track)
    {
        Stop();
        var proto = _proto.Index(track);
        var resolved = new ResolvedPathSpecifier(proto.Path.Path.ToString());
        var playing = _audio.PlayGlobal(resolved, Robust.Shared.Player.Filter.Local(), recordReplay: false, audioParams: AudioParams.Default);
        if (playing != null)
        {
            _currentAudio = playing.Value.Entity;
            _window?.SetAudioStream(_currentAudio);
            var len = _audio.GetAudioLength(resolved);
            _window?.SetNowPlaying(proto.Name, (float) len.TotalSeconds);
            SetVolume(_volume);
        }
    }

    private void Next()
    {
        if (_currentCategory == null)
            return;
        var list = _tracksByCat.GetValueOrDefault(_currentCategory.Value, new());
        if (list.Count == 0)
            return;

        ProtoId<MusicTrackPrototype>? next;
        if (_currentTrack == null)
            next = list[0];
        else
        {
            var idx = list.IndexOf(_currentTrack.Value);
            if (idx < 0 || idx + 1 >= list.Count)
                next = list[0];
            else
                next = list[idx + 1];
        }

        if (next != null)
        {
            _currentTrack = next.Value;
            Play(next.Value);
        }
    }
}
