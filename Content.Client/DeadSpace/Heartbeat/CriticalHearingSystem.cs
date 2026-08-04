using Content.Client.Audio;
using Content.Client.DeadSpace.Instruments;
using Content.Shared.DeadSpace.Heartbeat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Client.DeadSpace.Heartbeat;

public sealed class CriticalHearingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CritHeartbeatSystem _heartbeat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const float FadeOutDuration = 0.2f;
    private const float FadeInDuration = 0.25f;

    private float _worldGain = 1f;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        UpdatesAfter.Add(typeof(AudioSystem));
        UpdatesAfter.Add(typeof(CritHeartbeatSystem));
        UpdatesBefore.Add(typeof(NoiseCancellingClientSystem));

        SubscribeLocalEvent<AudioComponent, BeforeAudioSourceInitializeEvent>(OnBeforeAudioSourceInitialize);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var targetGain = ShouldMuteWorld() ? 0f : 1f;
        var fadeDuration = targetGain < _worldGain ? FadeOutDuration : FadeInDuration;
        var gainStep = frameTime / fadeDuration;
        _worldGain = targetGain < _worldGain
            ? MathF.Max(targetGain, _worldGain - gainStep)
            : MathF.Min(targetGain, _worldGain + gainStep);

        if (_worldGain >= 1f)
        {
            RestoreVolumes();
            return;
        }

        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var audio))
        {
            if (HasComp<CriticalInternalAudioComponent>(uid))
                continue;

            var muted = GetOrCreateMutedAudio(uid, audio);
            ApplyVolume(uid, audio, muted);
        }
    }

    private void OnBeforeAudioSourceInitialize(
        EntityUid uid,
        AudioComponent audio,
        ref BeforeAudioSourceInitializeEvent args)
    {
        if (_heartbeat.CreatingInternalAudio ||
            (!ShouldMuteWorld() && _worldGain >= 1f))
        {
            return;
        }

        var muted = GetOrCreateMutedAudio(uid, audio);
        ApplyVolume(uid, audio, muted);
    }

    private CriticalMutedAudioComponent GetOrCreateMutedAudio(EntityUid uid, AudioComponent audio)
    {
        if (TryComp<CriticalMutedAudioComponent>(uid, out var muted))
        {
            UpdateOriginalVolume(audio, muted);
            return muted;
        }

        muted = AddComp<CriticalMutedAudioComponent>(uid);
        muted.OriginalVolume = GetConfiguredVolume(audio);
        muted.AppliedVolume = muted.OriginalVolume;
        return muted;
    }

    private void ApplyVolume(EntityUid uid, AudioComponent audio, CriticalMutedAudioComponent muted)
    {
        UpdateOriginalVolume(audio, muted);

        var attenuation = SharedAudioSystem.GainToVolume(_worldGain);
        var volume = muted.OriginalVolume + attenuation;
        _audio.SetVolume(uid, volume, audio);
        muted.AppliedVolume = volume;
    }

    private void RestoreVolumes()
    {
        var query = EntityQueryEnumerator<AudioComponent, CriticalMutedAudioComponent>();
        while (query.MoveNext(out var uid, out var audio, out var muted))
        {
            UpdateOriginalVolume(audio, muted);
            _audio.SetVolume(uid, muted.OriginalVolume, audio);
            RemCompDeferred<CriticalMutedAudioComponent>(uid);
        }
    }

    private static void UpdateOriginalVolume(AudioComponent audio, CriticalMutedAudioComponent muted)
    {
        var configured = GetConfiguredVolume(audio);
        if (!configured.Equals(muted.AppliedVolume))
            muted.OriginalVolume = configured;
    }

    private static float GetConfiguredVolume(AudioComponent audio)
    {
#pragma warning disable RA0002 // Preserve the source volume while applying the client-only critical mix.
        return audio.Params.Volume;
#pragma warning restore RA0002
    }

    private bool ShouldMuteWorld()
    {
        return _player.LocalEntity is { } player &&
               HasComp<CritHeartbeatComponent>(player) &&
               TryComp<MobStateComponent>(player, out var mobState) &&
               mobState.CurrentState is MobState.PreCritical or MobState.Critical;
    }
}
