using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private EntityUid? _ambientLoopStream;
    private AmbientLoopPrototype? _loopProto;
    private readonly TimeSpan _ambientLoopUpdateTime = TimeSpan.FromSeconds(5f);

    private const float AmbientLoopFadeInTime = 5f;
    private const float AmbientLoopFadeOutTime = 10f;
    private TimeSpan _nextLoop;

    private void InitializeAmbientLoop()
    {
        Subs.CVar(_configManager, CCVars.AmbientMusicVolume, AmbienceCVarChangedAmbientLoop, true);

        _nextAudio = TimeSpan.Zero;
    }

    private void AmbienceCVarChangedAmbientLoop(float obj)
    {
        _volumeSlider = SharedAudioSystem.GainToVolume(obj);

        if (_ambientLoopStream != null && _loopProto != null)
        {
            _audio.SetVolume(_ambientLoopStream, _loopProto.Sound.Params.Volume + _volumeSlider);
        }
    }

    private void UpdateAmbientLoop()
    {
        if (_timing.CurTime < _nextLoop)
            return;

        _nextLoop = _timing.CurTime + _ambientLoopUpdateTime;

        if (_state.CurrentState is not GameplayState)
        {
            _ambientLoopStream = Audio.Stop(_ambientLoopStream);
            _loopProto = null;
            return;
        }

        var currentLoopProto = GetAmbientLoop();
        if (currentLoopProto == null)
            return;
        if (_loopProto == null)
        {
            ChangeAmbientLoop(currentLoopProto);
        }
        else if (_loopProto.ID != currentLoopProto.ID)
        {
            ChangeAmbientLoop(currentLoopProto);
        }
    }

    private void ChangeAmbientLoop(AmbientLoopPrototype newProto)
    {
        if (!TryGetAudioLoops(newProto.Sound, out var loopsResPaths))
            return;

        _loopProto = newProto;
        FadeOut(_ambientLoopStream, duration: AmbientLoopFadeOutTime);
        var loopResPath = _random.Pick(loopsResPaths).ToString();
        var newLoop = _audio.PlayGlobal(
            loopResPath,
            Filter.Local(),
            false,
            AudioParams.Default
                .WithLoop(true)
                .WithVolume(_loopProto.Sound.Params.Volume + _volumeSlider)
        );
        _ambientLoopStream = newLoop.Value.Entity;
        FadeIn(_ambientLoopStream, newLoop.Value.Component, AmbientLoopFadeInTime);
    }

    private bool TryGetAudioLoops(SoundSpecifier sound, out List<ResPath> sounds)
    {
        sounds = new();
        switch (sound)
        {
            case SoundCollectionSpecifier collection:
                if (collection.Collection == null)
                    return false;

                var slothCud = _proto.Index<SoundCollectionPrototype>(collection.Collection);

                sounds.AddRange(slothCud.PickFiles);
                break;

            case SoundPathSpecifier path:
                sounds.Add(path.Path);
                break;
        }

        return true;
    }

    private AmbientLoopPrototype? GetAmbientLoop()
    {
        var player = _player.LocalEntity;

        if (player == null)
            return null;

        var ambientLoops = _proto.EnumeratePrototypes<AmbientLoopPrototype>().ToList();
        ambientLoops.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var loop in ambientLoops)
        {
            if (!_rules.IsTrue(player.Value, _proto.Index<RulesPrototype>(loop.Rules)))
                continue;

            return loop;
        }

        _sawmill.Warning("Unable to find fallback ambience loop");
        return null;
    }
}
