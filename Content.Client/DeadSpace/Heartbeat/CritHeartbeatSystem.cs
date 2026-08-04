// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.Heartbeat;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.DeadSpace.Heartbeat;

public sealed class CritHeartbeatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityUid? _trackedEntity;
    private EntityUid? _currentStream;
    private EntityUid? _ringingStream;
    private MobState _lastState = MobState.Invalid;
    private TimeSpan _nextBeat;
    private TimeSpan _nextRinging;

    public float VisualPulse { get; private set; }
    public bool CreatingInternalAudio { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(_ => Reset());
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(_ => Reset());
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Reset());
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        VisualPulse *= MathF.Exp(-6f * frameTime);
        if (VisualPulse < 0.001f)
            VisualPulse = 0f;

        if (_player.LocalEntity is not { } player ||
            !TryComp<CritHeartbeatComponent>(player, out var heartbeat) ||
            !TryComp<MobStateComponent>(player, out var mobState) ||
            !TryComp<DamageableComponent>(player, out var damageable) ||
            !TryComp<MobThresholdsComponent>(player, out var thresholds))
        {
            Reset();
            return;
        }

        if (_trackedEntity != player)
        {
            Reset();
            _trackedEntity = player;
            _lastState = mobState.CurrentState;
            _nextBeat = _timing.RealTime;
            ScheduleRinging(mobState.CurrentState);
        }

        if (_lastState != mobState.CurrentState)
            HandleStateChange(heartbeat, mobState.CurrentState);

        if (mobState.CurrentState is not (MobState.PreCritical or MobState.Critical))
            return;

        UpdateRinging(heartbeat, mobState.CurrentState);

        if (_timing.RealTime < _nextBeat)
            return;

        var beat = GetBeat(player, heartbeat, mobState.CurrentState, damageable, thresholds);
        PlayBeat(beat.Sound, beat.Volume, beat.Pitch, beat.VisualIntensity);
        _nextBeat = _timing.RealTime + TimeSpan.FromSeconds(60f / beat.Bpm);
    }

    private void HandleStateChange(CritHeartbeatComponent heartbeat, MobState newState)
    {
        var oldState = _lastState;
        _lastState = newState;
        _nextBeat = _timing.RealTime;
        StopSound();
        StopRinging();
        ScheduleRinging(newState);

        if (newState != MobState.Dead || oldState is MobState.Invalid or MobState.Dead)
            return;

        PlayBeat(heartbeat.DeathSound, -9f, 1f, 1f);
    }

    private HeartbeatBeat GetBeat(
        EntityUid player,
        CritHeartbeatComponent heartbeat,
        MobState state,
        DamageableComponent damageable,
        MobThresholdsComponent thresholds)
    {
        var damage = damageable.TotalDamage.Float();

        if (state == MobState.PreCritical)
        {
            var progress = GetStateProgress(
                player,
                MobState.PreCritical,
                MobState.Critical,
                damage,
                thresholds);

            return new HeartbeatBeat(
                heartbeat.PreCriticalSound,
                MathHelper.Lerp(112f, 144f, progress),
                MathHelper.Lerp(-12f, -8f, progress),
                MathHelper.Lerp(1.05f, 1.16f, progress),
                MathHelper.Lerp(0.45f, 0.7f, progress));
        }

        var criticalProgress = GetStateProgress(
            player,
            MobState.Critical,
            MobState.Dead,
            damage,
            thresholds);

        return new HeartbeatBeat(
            heartbeat.CriticalSound,
            MathHelper.Lerp(78f, 36f, criticalProgress),
            MathHelper.Lerp(-11f, -7f, criticalProgress),
            MathHelper.Lerp(0.9f, 0.72f, criticalProgress),
            MathHelper.Lerp(0.75f, 1f, criticalProgress));
    }

    private float GetStateProgress(
        EntityUid player,
        MobState startState,
        MobState endState,
        float damage,
        MobThresholdsComponent thresholds)
    {
        if (!_thresholds.TryGetThresholdForState(player, startState, out var start, thresholds) ||
            !_thresholds.TryGetThresholdForState(player, endState, out var end, thresholds))
        {
            return 0f;
        }

        var startValue = start.Value.Float();
        var endValue = end.Value.Float();
        if (MathHelper.CloseTo(startValue, endValue))
            return 0f;

        return Math.Clamp((damage - startValue) / (endValue - startValue), 0f, 1f);
    }

    private void PlayBeat(SoundSpecifier sound, float volume, float pitch, float visualIntensity)
    {
        StopSound();

        var audioParams = sound.Params
            .WithVolume(sound.Params.Volume + volume)
            .WithPitchScale(pitch);
        _currentStream = PlayInternalSound(sound, audioParams);

        VisualPulse = MathF.Max(VisualPulse, visualIntensity);
    }

    private void UpdateRinging(CritHeartbeatComponent heartbeat, MobState state)
    {
        if (_timing.RealTime < _nextRinging)
            return;

        StopRinging();

        var volume = state == MobState.PreCritical
            ? _random.NextFloat(-20f, -17f)
            : _random.NextFloat(-18f, -15f);
        var audioParams = heartbeat.EarRingingSound.Params
            .WithVolume(heartbeat.EarRingingSound.Params.Volume + volume)
            .WithPitchScale(_random.NextFloat(0.96f, 1.04f));
        _ringingStream = PlayInternalSound(heartbeat.EarRingingSound, audioParams);
        ScheduleRinging(state);
    }

    private EntityUid? PlayInternalSound(SoundSpecifier sound, AudioParams audioParams)
    {
        EntityUid? stream;
        CreatingInternalAudio = true;
        try
        {
            stream = _audio.PlayGlobal(sound, Filter.Local(), false, audioParams)?.Entity;
        }
        finally
        {
            CreatingInternalAudio = false;
        }

        if (stream is { } uid)
            EnsureComp<CriticalInternalAudioComponent>(uid);

        return stream;
    }

    private void ScheduleRinging(MobState state)
    {
        var delay = state switch
        {
            MobState.PreCritical => _random.NextFloat(16f, 26f),
            MobState.Critical => _random.NextFloat(10f, 19f),
            _ => 0f,
        };

        _nextRinging = delay > 0f
            ? _timing.RealTime + TimeSpan.FromSeconds(delay)
            : TimeSpan.Zero;
    }

    private void StopSound()
    {
        _currentStream = _audio.Stop(_currentStream);
    }

    private void StopRinging()
    {
        _ringingStream = _audio.Stop(_ringingStream);
    }

    private void Reset()
    {
        StopSound();
        StopRinging();
        _trackedEntity = null;
        _lastState = MobState.Invalid;
        _nextBeat = TimeSpan.Zero;
        _nextRinging = TimeSpan.Zero;
        VisualPulse = 0f;
        CreatingInternalAudio = false;
    }

    private readonly record struct HeartbeatBeat(
        SoundSpecifier Sound,
        float Bpm,
        float Volume,
        float Pitch,
        float VisualIntensity);
}
