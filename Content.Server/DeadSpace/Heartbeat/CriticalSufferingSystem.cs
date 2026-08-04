using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.Heartbeat;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Heartbeat;

public sealed class CriticalSufferingSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    private static readonly SoundSpecifier MaleGasp = new SoundCollectionSpecifier("CriticalSufferingMaleGasp");
    private static readonly SoundSpecifier FemaleGasp = new SoundCollectionSpecifier("CriticalSufferingFemaleGasp");
    private static readonly SoundSpecifier MaleGroan = new SoundCollectionSpecifier("CriticalSufferingMaleGroan");
    private static readonly SoundSpecifier FemaleGroan = new SoundCollectionSpecifier("CriticalSufferingFemaleGroan");
    private static readonly SoundSpecifier MaleCough = new SoundCollectionSpecifier("CriticalSufferingMaleCough");
    private static readonly SoundSpecifier FemaleCough = new SoundCollectionSpecifier("CriticalSufferingFemaleCough");
    private static readonly SoundSpecifier MaleRetch = new SoundCollectionSpecifier("CriticalSufferingMaleRetch");
    private static readonly SoundSpecifier FemaleRetch = new SoundCollectionSpecifier("CriticalSufferingFemaleRetch");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CritHeartbeatComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CritHeartbeatComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<CritHeartbeatComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<MobStateComponent>(ent, out var mobState) && IsSuffering(mobState.CurrentState))
            StartEpisode(ent, mobState.CurrentState);
    }

    private void OnMobStateChanged(EntityUid uid, CritHeartbeatComponent component, MobStateChangedEvent args)
    {
        if (!IsSuffering(args.NewMobState))
        {
            RemComp<CriticalSufferingComponent>(uid);
            return;
        }

        if (!IsSuffering(args.OldMobState) || !TryComp<CriticalSufferingComponent>(uid, out var suffering))
        {
            StartEpisode(uid, args.NewMobState);
            return;
        }

        var soon = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(1f, 2.5f));
        if (suffering.NextSymptom > soon)
            suffering.NextSymptom = soon;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CriticalSufferingComponent, MobStateComponent, DamageableComponent,
            MobThresholdsComponent>();

        while (query.MoveNext(out var uid, out var suffering, out var mobState, out var damage, out var thresholds))
        {
            if (Paused(uid))
                continue;

            if (!IsSuffering(mobState.CurrentState) || !HasComp<CritHeartbeatComponent>(uid))
            {
                RemCompDeferred<CriticalSufferingComponent>(uid);
                continue;
            }

            var depth = GetStateDepth(uid, mobState.CurrentState, damage, thresholds);

            if (suffering.VomitPending && suffering.NextVomit <= now)
            {
                suffering.VomitPending = false;
                _vomit.Vomit(uid);
                DelayOtherSymptoms(suffering, now);
                continue;
            }

            if (suffering.NextSymptom <= now)
            {
                TriggerSymptom(uid, suffering, mobState.CurrentState);
                ScheduleSymptom(suffering, mobState.CurrentState, depth, now);
            }

            if (suffering.NextJitter <= now)
            {
                TriggerJitter(uid, mobState.CurrentState, depth);
                ScheduleJitter(suffering, mobState.CurrentState, depth, now);
            }
        }
    }

    private void StartEpisode(EntityUid uid, MobState state)
    {
        var suffering = EnsureComp<CriticalSufferingComponent>(uid);
        var now = _timing.CurTime;

        suffering.LastSymptom = CriticalSymptom.None;
        suffering.NextSymptom = now + TimeSpan.FromSeconds(_random.NextFloat(1.5f, 3f));
        suffering.NextJitter = now + TimeSpan.FromSeconds(_random.NextFloat(4f, 8f));
        suffering.VomitPending = _random.Prob(state == MobState.PreCritical ? 0.15f : 0.08f);
        suffering.NextVomit = suffering.VomitPending
            ? now + TimeSpan.FromSeconds(_random.NextFloat(5f, 12f))
            : TimeSpan.Zero;
    }

    private void TriggerSymptom(
        EntityUid uid,
        CriticalSufferingComponent suffering,
        MobState state)
    {
        var symptom = PickSymptom(state, suffering.LastSymptom);
        suffering.LastSymptom = symptom;

        var emote = symptom switch
        {
            CriticalSymptom.Gasp => "CriticalSufferingGasp",
            CriticalSymptom.Groan => "CriticalSufferingGroan",
            CriticalSymptom.Cough => "CriticalSufferingCough",
            CriticalSymptom.Retch => "CriticalSufferingRetch",
            _ => "CriticalSufferingGasp",
        };

        PlayVoice(uid, symptom, state);
        _chat.TryEmoteWithChat(
            uid,
            emote,
            ChatTransmitRange.Normal,
            hideLog: true,
            ignoreActionBlocker: true,
            forceEmote: true);
    }

    private CriticalSymptom PickSymptom(MobState state, CriticalSymptom previous)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var roll = _random.NextFloat();
            var symptom = state == MobState.PreCritical
                ? roll switch
                {
                    < 0.25f => CriticalSymptom.Gasp,
                    < 0.60f => CriticalSymptom.Groan,
                    < 0.85f => CriticalSymptom.Cough,
                    _ => CriticalSymptom.Retch,
                }
                : roll switch
                {
                    < 0.35f => CriticalSymptom.Gasp,
                    < 0.75f => CriticalSymptom.Groan,
                    < 0.90f => CriticalSymptom.Cough,
                    _ => CriticalSymptom.Retch,
                };

            if (symptom != previous)
                return symptom;
        }

        return previous == CriticalSymptom.Groan
            ? CriticalSymptom.Gasp
            : CriticalSymptom.Groan;
    }

    private void PlayVoice(EntityUid uid, CriticalSymptom symptom, MobState state)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        var female = appearance.Sex == Sex.Female;
        var sound = (symptom, female) switch
        {
            (CriticalSymptom.Gasp, false) => MaleGasp,
            (CriticalSymptom.Gasp, true) => FemaleGasp,
            (CriticalSymptom.Groan, false) => MaleGroan,
            (CriticalSymptom.Groan, true) => FemaleGroan,
            (CriticalSymptom.Cough, false) => MaleCough,
            (CriticalSymptom.Cough, true) => FemaleCough,
            (CriticalSymptom.Retch, false) => MaleRetch,
            (CriticalSymptom.Retch, true) => FemaleRetch,
            _ => female ? FemaleGasp : MaleGasp,
        };
        var volume = state == MobState.PreCritical
            ? _random.NextFloat(-3f, -1f)
            : _random.NextFloat(-4.5f, -2.5f);
        var audioParams = sound.Params
            .WithVolume(sound.Params.Volume + volume)
            .WithVariation(0.08f)
            .WithPitchScale(_random.NextFloat(0.94f, 1.03f));

        _audio.PlayPvs(sound, uid, audioParams);
    }

    private void TriggerJitter(EntityUid uid, MobState state, float depth)
    {
        var duration = state == MobState.PreCritical
            ? _random.NextFloat(3f, 5f)
            : _random.NextFloat(2.5f, 4.5f);
        var amplitude = state == MobState.PreCritical
            ? Lerp(18f, 35f, depth)
            : Lerp(22f, 10f, depth);
        var frequency = state == MobState.PreCritical
            ? _random.NextFloat(1.4f, 2.2f)
            : _random.NextFloat(1.1f, 1.8f);

        _jitter.DoJitter(
            uid,
            TimeSpan.FromSeconds(duration),
            true,
            amplitude,
            frequency);
    }

    private void ScheduleSymptom(
        CriticalSufferingComponent suffering,
        MobState state,
        float depth,
        TimeSpan now)
    {
        var minimum = state == MobState.PreCritical
            ? Lerp(11f, 8.5f, depth)
            : Lerp(10f, 8f, depth);
        var maximum = state == MobState.PreCritical
            ? Lerp(17f, 14f, depth)
            : Lerp(16f, 13f, depth);

        suffering.NextSymptom = now + TimeSpan.FromSeconds(_random.NextFloat(minimum, maximum));
    }

    private void ScheduleJitter(
        CriticalSufferingComponent suffering,
        MobState state,
        float depth,
        TimeSpan now)
    {
        var minimum = state == MobState.PreCritical
            ? Lerp(11f, 9f, depth)
            : Lerp(14f, 18f, depth);
        var maximum = state == MobState.PreCritical
            ? Lerp(19f, 15f, depth)
            : Lerp(23f, 28f, depth);

        suffering.NextJitter = now + TimeSpan.FromSeconds(_random.NextFloat(minimum, maximum));
    }

    private float GetStateDepth(
        EntityUid uid,
        MobState state,
        DamageableComponent damage,
        MobThresholdsComponent thresholds)
    {
        var endState = state == MobState.PreCritical ? MobState.Critical : MobState.Dead;
        if (!_thresholds.TryGetThresholdForState(uid, state, out var start, thresholds) ||
            !_thresholds.TryGetThresholdForState(uid, endState, out var end, thresholds))
        {
            return 0f;
        }

        var startValue = start.Value.Float();
        var endValue = end.Value.Float();
        if (MathF.Abs(endValue - startValue) < 0.001f)
            return 0f;

        return Math.Clamp((damage.TotalDamage.Float() - startValue) / (endValue - startValue), 0f, 1f);
    }

    private static void DelayOtherSymptoms(CriticalSufferingComponent suffering, TimeSpan now)
    {
        var resume = now + TimeSpan.FromSeconds(4f);
        if (suffering.NextSymptom < resume)
            suffering.NextSymptom = resume;
        if (suffering.NextJitter < resume)
            suffering.NextJitter = resume;
    }

    private static bool IsSuffering(MobState state)
    {
        return state is MobState.PreCritical or MobState.Critical;
    }

    private static float Lerp(float from, float to, float amount)
    {
        return from + (to - from) * amount;
    }
}
