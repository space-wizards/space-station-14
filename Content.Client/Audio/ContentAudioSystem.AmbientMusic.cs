using System.Linq;
using Content.Shared.Audio;
using Content.Shared.GameTicking;
using Content.Shared.Random;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RulesSystem _rules = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private AudioSystem.PlayingStream? _ambientMusicStream;

    private readonly TimeSpan _minAmbienceTime = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _maxAmbienceTime = TimeSpan.FromSeconds(60);

    // Don't need to worry about this being serializable or pauseable as it doesn't affect the sim.
    private TimeSpan _nextAudio;

    /// <summary>
    /// Track what ambient sounds we've played. This is so they all get played an even
    /// number of times.
    /// When we get to the end of the list we'll re-shuffle
    /// </summary>
    private readonly Dictionary<string, List<ResPath>> _ambientSounds = new();

    private ISawmill _sawmill = default!;

    private void InitializeAmbientMusic()
    {
        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("audio.ambience");

        // Reset audio
        _nextAudio = TimeSpan.Zero;

        // TODO: On round end summary OR lobby cut audio.
        SetupAmbientSounds();
        _proto.PrototypesReloaded += OnProtoReload;
    }

    private void ShutdownAmbientMusic()
    {
        _proto.PrototypesReloaded -= OnProtoReload;
        _ambientMusicStream?.Stop();
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

    private void SetupAmbientSounds()
    {
        foreach (var ambience in _proto.EnumeratePrototypes<AmbientMusicPrototype>())
        {
            var tracks = _ambientSounds.GetOrNew(ambience.ID);
            RefreshTracks(ambience.Sound, tracks, null);
            _random.Shuffle(tracks);
        }
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_ambientMusicStream?.Done == false || !_timing.IsFirstTimePredicted)
            return;

        _ambientMusicStream = null;

        if (_nextAudio > _timing.CurTime)
            return;

        // Also don't need to worry about rounding here as it doesn't affect the sim
        _nextAudio = _timing.CurTime + _random.Next(_minAmbienceTime, _maxAmbienceTime);

        var ambience = GetAmbience();

        if (ambience == null)
            return;

        var tracks = _ambientSounds[ambience.ID];

        var track = tracks[^1];
        tracks.RemoveAt(tracks.Count - 1);

        var strim = _audio.PlayGlobal(track.ToString(), Filter.Local(), false, AudioParams.Default.WithVolume(-12));

        if (strim != null)
        {
            _ambientMusicStream = (AudioSystem.PlayingStream) strim;
        }

        // Refresh the list
        if (tracks.Count == 0)
        {
            RefreshTracks(ambience.Sound, tracks, track);
        }
    }

    private AmbientMusicPrototype? GetAmbience()
    {
        var ambiences = _proto.EnumeratePrototypes<AmbientMusicPrototype>().ToList();

        ambiences.Sort((x, y) => (y.Priority.CompareTo(x.Priority)));

        foreach (var amb in ambiences)
        {
            if (!_rules.IsTrue(_proto.Index<RulesPrototype>(amb.Rules)))
                continue;

            return amb;
        }

        _sawmill.Warning($"Unable to find fallback ambience track");
        return null;
    }
}
