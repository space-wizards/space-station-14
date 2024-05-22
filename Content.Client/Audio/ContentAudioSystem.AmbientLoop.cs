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

/// <summary>
/// A system that constantly plays a quiet background ambient depending on the player's current environment.
/// An endless looped track or collection is played, and seamlessly changes to another if the player's
/// environment is changed.
/// </summary>
public sealed partial class ContentAudioSystem
{
    private EntityUid? _ambientLoopStream;
    private AmbientLoopPrototype? _loopProto;
    private readonly TimeSpan _ambientLoopUpdateTime = TimeSpan.FromSeconds(5f);

    private const float AmbientLoopFadeInTime = 5f;
    private const float AmbientLoopFadeOutTime = 8f;
    private TimeSpan _nextLoop = TimeSpan.Zero;

    private void InitializeAmbientLoop()
    {
        //Subscribe to sound volume changes in the settings menu
        //The already existing ambient volume parameter is used
        Subs.CVar(_configManager, CCVars.AmbientMusicVolume, AmbienceCVarChangedAmbientLoop, true);
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
        //Optimization: the environment check code is not run every frame, but with a certain periodicity
        if (_timing.CurTime < _nextLoop)
            return;
        _nextLoop = _timing.CurTime + _ambientLoopUpdateTime;

        //We don't play background ambient in the lobby.
        if (_state.CurrentState is not GameplayState)
        {
            _ambientLoopStream = Audio.Stop(_ambientLoopStream);
            _loopProto = null;
            return;
        }

        //If now there is no background ambient, or it is different from what should play in the current conditions -
        //we start the process of smooth transition to another ambient
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

    /// <summary>
    /// Smoothly turns off the current ambient, and smoothly turns on the new ambient
    /// </summary>
    private void ChangeAmbientLoop(AmbientLoopPrototype newProto)
    {
        FadeOut(_ambientLoopStream, duration: AmbientLoopFadeOutTime);

        _loopProto = newProto;
        var newLoop = _audio.PlayGlobal(
            newProto.Sound,
            Filter.Local(),
            false,
            AudioParams.Default
                .WithLoop(true)
                .WithVolume(_loopProto.Sound.Params.Volume + _volumeSlider)
                .WithPlayOffset(_random.NextFloat(0.0f, 100.0f))
        );
        _ambientLoopStream = newLoop.Value.Entity;

        FadeIn(_ambientLoopStream, newLoop.Value.Component, AmbientLoopFadeInTime);
    }

    /// <summary>
    /// Checking the player's environment through the rules. Returns the current ambient to be played.
    /// </summary>
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
