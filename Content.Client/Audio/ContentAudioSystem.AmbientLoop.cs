using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private EntityUid? _ambientLoopStream;
    private AmbientLoopPrototype? _loopProto;

    private readonly TimeSpan AmbientLoopUpdateTime = TimeSpan.FromSeconds(5f);
    private const float AmbientLoopFadeTime = 5f;
    private TimeSpan _nextLoop;
    private void InitializeAmbientLoop()
    {
        //TODO: Add CVar to ambient loop

        //Reset Loop
        _nextAudio = TimeSpan.Zero;
        SetupAmbienceLoop();

    }

    private void SetupAmbienceLoop()
    {
        foreach (var ambienceLoop in _proto.EnumeratePrototypes<AmbientLoopPrototype>())
        {
            var loops = _ambientSounds.GetOrNew(ambienceLoop.ID);

            DebugTools.Assert(loops.Count == 0);

        }
    }

    private void UpdateAmbientLoop()
    {
        if (_timing.CurTime < _nextLoop)
            return;

        _nextLoop = _timing.CurTime + AmbientLoopUpdateTime;

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
        else if (_loopProto.ToString() != currentLoopProto.ToString())
        {
            ChangeAmbientLoop(currentLoopProto);
        }
    }

    private void ChangeAmbientLoop(AmbientLoopPrototype newProto)
    {
        if (!TryGetAudioLoops(newProto.Sound, out var loopsResPaths))
            return;

        FadeOut(_ambientLoopStream, duration: AmbientLoopFadeTime);
        var loopResPath = _random.Pick(loopsResPaths).ToString();
        var newLoop = _audio.PlayGlobal(
            loopResPath,
            Filter.Local(),
            false,
            AudioParams.Default.WithLoop(true) //TODO Volume CVAR
        );
        _ambientLoopStream = newLoop.Value.Entity;
        FadeIn(_ambientLoopStream, newLoop.Value.Component, AmbientLoopFadeTime);
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

        var ambiLoops = _proto.EnumeratePrototypes<AmbientLoopPrototype>().ToList();
        ambiLoops.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var loop in ambiLoops)
        {
            if (!_rules.IsTrue(player.Value, _proto.Index<RulesPrototype>(loop.Rules)))
                continue;

            return loop;
        }

        _sawmill.Warning("Unable to find fallback ambience loop");
        return null;
    }
}
