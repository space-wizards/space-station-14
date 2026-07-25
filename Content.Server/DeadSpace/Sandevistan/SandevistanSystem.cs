// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Sandevistan;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Sandevistan;

public sealed class SandevistanSystem : EntitySystem
{
    private static readonly TimeSpan ExhaustionStaminaCritBufferTime = TimeSpan.FromSeconds(3f);
    private static readonly TimeSpan ImplantTraumaDuration = TimeSpan.FromSeconds(10f);
    private static readonly TimeSpan ImplantEmoteInterval = TimeSpan.FromSeconds(2.5f);
    private static readonly TimeSpan ImplantAttemptJitterRefresh = TimeSpan.FromSeconds(0.5f);
    private static readonly TimeSpan ImplantTraumaJitterRefresh = TimeSpan.FromSeconds(0.35f);
    private static readonly TimeSpan ImplantAttemptCompletionGrace = TimeSpan.FromSeconds(1f);
    private const float VisualFadeDuration = 2.5f;
    private const float SoftcapRampLeadTime = 2f;
    private const float ImplantJitterStartAmplitude = 1.5f;
    private const float ImplantJitterStartFrequency = 4f;
    private const float ImplantJitterAmplitude = 7.5f;
    private const float ImplantJitterFrequency = 10f;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanImplantComponent, ActivateSandevistanImplantEvent>(OnActivated);
        SubscribeLocalEvent<SandevistanImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<SandevistanImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
        SubscribeLocalEvent<SandevistanImplantComponent, ComponentShutdown>(OnImplantShutdown);
        SubscribeLocalEvent<SandevistanImplanterComponent, DoAfterAttemptEvent<ImplantEvent>>(OnImplantAttempt);
        SubscribeLocalEvent<SandevistanImplanterComponent, ImplantEvent>(OnImplantAttemptFinished);
        SubscribeLocalEvent<SandevistanImplantTraumaComponent, RejuvenateEvent>(OnImplantTraumaRejuvenated);
        SubscribeLocalEvent<ActiveSandevistanComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ImplantedComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveSandevistanComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (Paused(uid))
                continue;

            if (IsCriticalOrDead(uid))
            {
                RequestStop(uid, active, curTime);
                StopSandevistan(uid, active, curTime);
                continue;
            }

            if (ShouldStop(uid, active) || curTime >= active.EndTime)
            {
                StopSandevistan(uid, active, curTime);
                continue;
            }

            if (IsStaminaExhausted(uid))
            {
                StopSandevistan(uid, active, curTime);
                continue;
            }

            StartWorkingSoundIfReady(uid, active, curTime);
            ApplyJitter(uid, active, frameTime);
            ApplySoftcapFeedback(uid, active, curTime);
            ApplyEndWarningFeedback(uid, active, curTime);

            if (curTime >= active.NextOverloadTime)
                ApplyOverloadTicks(uid, active, curTime);
        }

        UpdateRecovery(curTime);
        UpdateVisualFadeouts(curTime);
        UpdateImplantTrauma(curTime);
    }

    private void OnImplantAttempt(
        Entity<SandevistanImplanterComponent> ent,
        ref DoAfterAttemptEvent<ImplantEvent> args)
    {
        if (args.DoAfter.Args.Target is not { } target || Deleted(target))
            return;

        var curTime = _timing.CurTime;
        if (ent.Comp.ActiveDoAfter != args.DoAfter.Id)
        {
            ent.Comp.ActiveDoAfter = args.DoAfter.Id;
            ent.Comp.NextScreamTime = curTime;
        }

        var duration = Math.Max(args.DoAfter.Args.Delay.TotalSeconds, 0.1);
        var trauma = EnsureComp<SandevistanImplantTraumaComponent>(target);
        if (trauma.ActiveDoAfter != args.DoAfter.Id || trauma.Implanted)
        {
            trauma.ActiveDoAfter = args.DoAfter.Id;
            trauma.StartTime = args.DoAfter.StartTime;
            trauma.EndTime = args.DoAfter.StartTime + args.DoAfter.Args.Delay;
            trauma.Duration = (float) duration;
            trauma.Implanted = false;
        }

        var progress = Math.Clamp((curTime - args.DoAfter.StartTime).TotalSeconds / duration, 0d, 1d);
        ApplyImplantAttemptJitter(target, (float) progress);

        if (curTime < ent.Comp.NextScreamTime)
            return;

        _chat.TryEmoteWithChat(target, "Scream", ignoreActionBlocker: true, forceEmote: true);
        ent.Comp.NextScreamTime = curTime + ImplantEmoteInterval;
    }

    private void OnImplantAttemptFinished(Entity<SandevistanImplanterComponent> ent, ref ImplantEvent args)
    {
        if (ent.Comp.ActiveDoAfter == args.DoAfter.Id)
            ent.Comp.ActiveDoAfter = null;

        if (!args.Cancelled ||
            args.Target is not { } target ||
            !TryComp<SandevistanImplantTraumaComponent>(target, out var trauma) ||
            trauma.ActiveDoAfter != args.DoAfter.Id ||
            trauma.Implanted)
        {
            return;
        }

        RemCompDeferred<SandevistanImplantTraumaComponent>(target);
    }

    private void OnImplanted(Entity<SandevistanImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        var target = args.Implanted;
        if (Deleted(target))
            return;

        if (TryComp<DamageableComponent>(target, out var damageable))
        {
            var damage = new DamageSpecifier
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Slash", 5 },
                    { "Piercing", 5 },
                },
            };

            _damageable.TryChangeDamage(
                (target, damageable),
                damage,
                ignoreResistances: true,
                interruptsDoAfters: false,
                origin: ent.Owner,
                ignoreGlobalModifiers: true);
        }

        _stun.TryUpdateParalyzeDuration(target, ImplantTraumaDuration);

        var curTime = _timing.CurTime;
        var trauma = EnsureComp<SandevistanImplantTraumaComponent>(target);
        trauma.ActiveDoAfter = null;
        trauma.StartTime = curTime;
        trauma.EndTime = curTime + ImplantTraumaDuration;
        trauma.Duration = (float) ImplantTraumaDuration.TotalSeconds;
        trauma.Implanted = true;
        trauma.NextEmoteTime = curTime;
        trauma.LaughNext = false;

        StartImplantTraumaVisual(target, ent.Comp, trauma.EndTime);
    }

    private void OnImplantTraumaRejuvenated(
        Entity<SandevistanImplantTraumaComponent> ent,
        ref RejuvenateEvent args)
    {
        RemCompDeferred<SandevistanImplantTraumaComponent>(ent);
    }

    private void UpdateImplantTrauma(TimeSpan curTime)
    {
        var query = EntityQueryEnumerator<SandevistanImplantTraumaComponent>();
        while (query.MoveNext(out var uid, out var trauma))
        {
            if (Paused(uid))
                continue;

            if (!trauma.Implanted)
            {
                if (curTime >= trauma.EndTime + ImplantAttemptCompletionGrace)
                {
                    RemCompDeferred<SandevistanImplantTraumaComponent>(uid);
                    continue;
                }

                var elapsed = MathF.Max(0f, (float) (curTime - trauma.StartTime).TotalSeconds);
                var progress = Math.Clamp(elapsed / MathF.Max(trauma.Duration, 0.1f), 0f, 1f);
                ApplyImplantAttemptJitter(uid, progress);
                continue;
            }

            if (curTime >= trauma.EndTime)
            {
                RemCompDeferred<SandevistanImplantTraumaComponent>(uid);
                continue;
            }

            var remaining = MathF.Max(0f, (float) (trauma.EndTime - curTime).TotalSeconds);
            var fade = SmoothStep(Math.Clamp(remaining / VisualFadeDuration, 0f, 1f));
            var jitterRefresh = TimeSpan.FromSeconds(MathF.Min(
                remaining,
                (float) ImplantTraumaJitterRefresh.TotalSeconds));
            ApplyImplantJitter(
                uid,
                ImplantJitterAmplitude * fade,
                ImplantJitterFrequency * fade,
                jitterRefresh);

            if (curTime < trauma.NextEmoteTime)
                continue;

            _chat.TryEmoteWithChat(
                uid,
                trauma.LaughNext ? "Laugh" : "Scream",
                ignoreActionBlocker: true,
                forceEmote: true);
            trauma.LaughNext = !trauma.LaughNext;
            trauma.NextEmoteTime = curTime + ImplantEmoteInterval;
        }
    }

    private void ApplyImplantAttemptJitter(EntityUid uid, float progress)
    {
        var ramp = SmoothStep(Math.Clamp(progress, 0f, 1f));
        ApplyImplantJitter(
            uid,
            MathHelper.Lerp(ImplantJitterStartAmplitude, ImplantJitterAmplitude, ramp),
            MathHelper.Lerp(ImplantJitterStartFrequency, ImplantJitterFrequency, ramp),
            ImplantAttemptJitterRefresh);
    }

    private void ApplyImplantJitter(EntityUid uid, float amplitude, float frequency, TimeSpan refreshTime)
    {
        _jittering.DoJitter(
            uid,
            refreshTime,
            true,
            amplitude,
            frequency,
            true);

        if (TryComp<JitteringComponent>(uid, out var jittering))
            Dirty(uid, jittering);
    }

    private void StartImplantTraumaVisual(
        EntityUid uid,
        SandevistanImplantComponent implant,
        TimeSpan endTime)
    {
        var fadeout = EnsureComp<SandevistanVisualFadeoutComponent>(uid);
        fadeout.EndTime = endTime;
        fadeout.Duration = VisualFadeDuration;
        fadeout.StartIntensity = 1f;
        fadeout.AllowRampIn = true;
        fadeout.SoftcapProgress = 1f;
        fadeout.AfterimageInterval = implant.AfterimageInterval;
        fadeout.AfterimageMinDistance = implant.AfterimageMinDistance;
        fadeout.AfterimageLifetime = implant.AfterimageLifetime;
        fadeout.AfterimageColor = implant.AfterimageColor;
        fadeout.AfterimageFallbackEffect = implant.AfterimageFallbackEffect;

        Dirty(uid, fadeout);
    }

    private void OnActivated(EntityUid uid, SandevistanImplantComponent component, ActivateSandevistanImplantEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SubdermalImplantComponent>(uid, out var subdermal) ||
            subdermal.ImplantedEntity is not { } target ||
            Deleted(target))
        {
            return;
        }

        var curTime = _timing.CurTime;
        if (TryComp<ActiveSandevistanComponent>(target, out var currentActive))
        {
            if (currentActive.SourceImplant == uid)
            {
                RequestStop(target, currentActive, curTime);

                args.Toggle = true;
                args.Handled = true;
            }

            return;
        }

        if (IsCriticalOrDead(target))
            return;

        if (curTime < component.NextReadyTime)
        {
            var seconds = MathF.Ceiling(MathF.Max(0f, (float) (component.NextReadyTime - curTime).TotalSeconds));
            PopupFollowing(
                target,
                Loc.GetString("sandevistan-implant-cooldown", ("seconds", seconds)),
                PopupType.SmallCaution);
            _actions.SetCooldown(args.Action.AsNullable(), curTime, component.NextReadyTime);
            return;
        }

        var active = EnsureComp<ActiveSandevistanComponent>(target);
        active.SourceImplant = uid;
        active.EndTime = curTime + TimeSpan.FromSeconds(component.Duration);
        active.SoftcapTime = curTime + TimeSpan.FromSeconds(component.SoftcapTime);
        active.NextOverloadTime = active.SoftcapTime;
        active.NextSoftcapPopupTime = curTime + GetInterval(component.SoftcapPopupInitialDelay);
        active.NextShoutTime = curTime + GetInterval(component.ShoutInitialDelay);
        active.CooldownMultiplier = component.CooldownMultiplier;
        active.StartTime = curTime;
        active.WorkingSoundStartTime = curTime + PlayActivationSound(target, component);
        active.WorkingSound = component.WorkingSound;
        active.DeactivationSound = component.DeactivationSound;
        active.WorkingSoundStarted = false;
        active.WorkingSoundStream = null;
        active.SoftcapPopupInterval = component.SoftcapPopupInterval;
        active.ShoutMinInterval = component.ShoutMinInterval;
        active.ShoutMaxInterval = component.ShoutMaxInterval;
        active.SoftcapPopups = new(component.SoftcapPopups);
        active.SoftcapShouts = new(component.SoftcapShouts);
        active.LastSoftcapPopupIndex = -1;
        active.LastSoftcapShoutIndex = -1;
        active.SoftcapPopupBag.Clear();
        active.SoftcapShoutBag.Clear();
        active.NextEndWarningTime = active.EndTime - TimeSpan.FromSeconds(component.EndWarningLeadTime);
        active.EndWarningLeadTime = component.EndWarningLeadTime;
        active.EndWarningInterval = component.EndWarningInterval;
        active.EndWarningPopupCount = component.EndWarningPopupCount;
        active.EndWarningPopups = new(component.EndWarningPopups);
        active.LastEndWarningPopupIndex = -1;
        active.EndWarningPopupBag.Clear();
        active.MovementSpeedModifier = component.MovementSpeedModifier;
        active.AttackRateModifier = component.AttackRateModifier;
        active.OverloadInterval = component.OverloadInterval;
        active.OverloadDamage = new(component.OverloadDamage);
        active.ExhaustionStaminaDamage = component.ExhaustionStaminaDamage;
        active.ManualStopRequested = false;
        active.ManualStopVisualIntensity = -1f;
        active.InitialJitterProgress = Math.Clamp(component.InitialJitterProgress, 0f, 1f);
        active.JitterCurrentProgress = 0f;
        active.JitterTargetProgress = active.InitialJitterProgress;
        active.JitterHits = 0;
        active.MaxJitterHits = component.MaxJitterHits;
        active.MaxJitterAmplitude = component.MaxJitterAmplitude;
        active.MaxJitterFrequency = component.MaxJitterFrequency;
        active.JitterLerpRate = component.JitterLerpRate;
        active.JitterRefreshTime = component.JitterRefreshTime;
        active.AfterimageInterval = component.AfterimageInterval;
        active.AfterimageMinDistance = component.AfterimageMinDistance;
        active.AfterimageLifetime = component.AfterimageLifetime;
        active.DeactivationVisualDuration = component.DeactivationVisualDuration;
        active.AfterimageColor = component.AfterimageColor;
        active.AfterimageFallbackEffect = component.AfterimageFallbackEffect;
        active.RecoveryTickInterval = component.RecoveryTickInterval;
        active.RecoveryDamage = new(component.RecoveryDamage);
        active.RecoveryJitterAmplitude = component.RecoveryJitterAmplitude;
        active.RecoveryJitterFrequency = component.RecoveryJitterFrequency;
        active.RecoveryJitterRefreshTime = component.RecoveryJitterRefreshTime;
        active.RecoveryPopupInterval = component.RecoveryPopupInterval;
        active.RecoveryPopups = new(component.RecoveryPopups);

        RemCompDeferred<SandevistanRecoveryComponent>(target);
        RemCompDeferred<SandevistanVisualFadeoutComponent>(target);
        Dirty(target, active);
        _movement.RefreshMovementSpeedModifiers(target);
        ApplyJitter(target, active, 0f);

        if (component.Popup is { } popup)
            PopupFollowing(target, Loc.GetString(popup), PopupType.Small);

        args.Toggle = true;
        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<ActiveSandevistanComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState < MobState.Critical)
            return;

        var curTime = _timing.CurTime;
        RequestStop(ent.Owner, ent.Comp, curTime);
        StopSandevistan(ent.Owner, ent.Comp, curTime);
    }

    private void OnRejuvenate(EntityUid uid, ImplantedComponent component, RejuvenateEvent args)
    {
        if (TryComp<ActiveSandevistanComponent>(uid, out var active))
            StopWorkingSound(active);

        RemComp<ActiveSandevistanComponent>(uid);
        RemComp<SandevistanRecoveryComponent>(uid);
        RemComp<SandevistanVisualFadeoutComponent>(uid);

        var query = EntityQueryEnumerator<SandevistanImplantComponent, SubdermalImplantComponent>();
        while (query.MoveNext(out var implant, out var sandevistan, out var subdermal))
        {
            if (subdermal.ImplantedEntity != uid)
                continue;

            sandevistan.NextReadyTime = TimeSpan.Zero;

            if (subdermal.Action is not { } action)
                continue;

            _actions.SetToggled(action, false);
            _actions.ClearCooldown(action);
        }
    }

    private void OnImplantRemoved(Entity<SandevistanImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (!TryComp<ActiveSandevistanComponent>(args.Implanted, out var active) ||
            active.SourceImplant != ent.Owner)
        {
            return;
        }

        StopSandevistan(args.Implanted);
    }

    private void OnImplantShutdown(Entity<SandevistanImplantComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SubdermalImplantComponent>(ent.Owner, out var subdermal) ||
            subdermal.ImplantedEntity is not { } target ||
            TerminatingOrDeleted(target) ||
            !TryComp<ActiveSandevistanComponent>(target, out var active) ||
            active.SourceImplant != ent.Owner)
        {
            return;
        }

        StopSandevistan(target);
    }

    private void OnMeleeHit(EntityUid uid, MeleeWeaponComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            HasComp<GunComponent>(args.Weapon) ||
            !TryComp<ActiveSandevistanComponent>(args.User, out var active))
        {
            return;
        }

        active.JitterHits = Math.Min(active.JitterHits + 1, Math.Max(active.MaxJitterHits, 1));

        var hitProgress = active.MaxJitterHits <= 0
            ? 1f
            : active.JitterHits / (float) active.MaxJitterHits;

        active.JitterTargetProgress = Math.Clamp(
            MathHelper.Lerp(active.InitialJitterProgress, 1f, hitProgress),
            active.InitialJitterProgress,
            1f);
    }

    private void RequestStop(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        active.ManualStopRequested = true;
        active.ManualStopVisualIntensity = GetActiveVisualIntensity(active, curTime);
        active.EndTime = curTime;
        Dirty(uid, active);
    }

    private bool IsCriticalOrDead(EntityUid uid)
    {
        return TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState >= MobState.Critical;
    }

    private bool ShouldStop(EntityUid uid, ActiveSandevistanComponent active)
    {
        if (active.SourceImplant is not { } implant ||
            Deleted(implant) ||
            !TryComp<SubdermalImplantComponent>(implant, out var subdermal) ||
            subdermal.ImplantedEntity != uid)
        {
            return true;
        }

        return false;
    }

    private void StopSandevistan(EntityUid uid)
    {
        if (!TryComp<ActiveSandevistanComponent>(uid, out var active))
            return;

        StopSandevistan(uid, active, _timing.CurTime);
    }

    private void StopSandevistan(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        var manualStop = active.ManualStopRequested;
        if (!manualStop)
            ApplyCompletionStaminaDamage(uid, active, curTime);

        StopWorkingSound(active);
        _audio.PlayEntity(active.DeactivationSound, uid, uid);

        active.MovementSpeedModifier = 1f;
        Dirty(uid, active);

        var downtime = GetDowntime(active, curTime);
        StartCooldown(active, curTime, downtime);
        StartVisualFadeout(uid, active, curTime);
        StartRecovery(uid, active, curTime, downtime);

        _movement.RefreshMovementSpeedModifiers(uid);

        RemCompDeferred<ActiveSandevistanComponent>(uid);
    }

    private TimeSpan PlayActivationSound(EntityUid uid, SandevistanImplantComponent component)
    {
        if (component.ActivationSounds.Count == 0)
            return GetInterval(component.WorkingSoundDelay);

        var index = _random.Next(component.ActivationSounds.Count);
        _audio.PlayEntity(component.ActivationSounds[index], uid, uid);

        var delay = index < component.ActivationSoundDurations.Count
            ? component.ActivationSoundDurations[index]
            : component.WorkingSoundDelay;

        return GetInterval(delay);
    }

    private void StartWorkingSoundIfReady(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        if (active.WorkingSoundStarted || curTime < active.WorkingSoundStartTime)
            return;

        active.WorkingSoundStarted = true;
        active.WorkingSoundStream = _audio
            .PlayEntity(active.WorkingSound, uid, uid, active.WorkingSound.Params.WithLoop(true))
            ?.Entity;
    }

    private void StopWorkingSound(ActiveSandevistanComponent active)
    {
        active.WorkingSoundStream = _audio.Stop(active.WorkingSoundStream);
    }

    private void StartVisualFadeout(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        if (active.DeactivationVisualDuration <= 0f)
            return;

        var startIntensity = active.ManualStopRequested && active.ManualStopVisualIntensity >= 0f
            ? active.ManualStopVisualIntensity
            : GetActiveVisualIntensity(active, curTime);
        if (startIntensity <= 0.01f)
            return;

        var fadeout = EnsureComp<SandevistanVisualFadeoutComponent>(uid);
        fadeout.Duration = active.DeactivationVisualDuration;
        fadeout.EndTime = curTime + TimeSpan.FromSeconds(active.DeactivationVisualDuration);
        fadeout.StartIntensity = startIntensity;
        fadeout.AllowRampIn = false;
        fadeout.SoftcapProgress = GetActiveSoftcapProgress(active, curTime);
        fadeout.AfterimageInterval = active.AfterimageInterval;
        fadeout.AfterimageMinDistance = active.AfterimageMinDistance;
        fadeout.AfterimageLifetime = active.AfterimageLifetime;
        fadeout.AfterimageColor = active.AfterimageColor;
        fadeout.AfterimageFallbackEffect = active.AfterimageFallbackEffect;

        Dirty(uid, fadeout);
    }

    private static TimeSpan GetDowntime(ActiveSandevistanComponent active, TimeSpan curTime)
    {
        var elapsed = MathF.Max(0f, (float) (curTime - active.StartTime).TotalSeconds);
        return TimeSpan.FromSeconds(elapsed * MathF.Max(active.CooldownMultiplier, 0f));
    }

    private void StartCooldown(ActiveSandevistanComponent active, TimeSpan curTime, TimeSpan cooldown)
    {
        if (active.SourceImplant is not { } implant ||
            Deleted(implant) ||
            !TryComp<SandevistanImplantComponent>(implant, out var implantComp))
        {
            return;
        }

        var cooldownEnd = curTime + cooldown;
        implantComp.NextReadyTime = cooldownEnd;

        if (!TryComp<SubdermalImplantComponent>(implant, out var subdermal) ||
            subdermal.Action is not { } action)
        {
            return;
        }

        _actions.SetToggled(action, false);

        if (cooldown > TimeSpan.Zero)
            _actions.SetCooldown(action, curTime, cooldownEnd);
        else
            _actions.ClearCooldown(action);
    }

    private void StartRecovery(
        EntityUid uid,
        ActiveSandevistanComponent active,
        TimeSpan curTime,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        var durationSeconds = (float) duration.TotalSeconds;
        var recovery = EnsureComp<SandevistanRecoveryComponent>(uid);
        recovery.Duration = durationSeconds;
        recovery.EndTime = curTime + duration;
        recovery.NextTickTime = curTime;
        recovery.NextPopupTime = curTime;
        recovery.TickInterval = active.RecoveryTickInterval;
        recovery.Damage = new(active.RecoveryDamage);
        recovery.JitterAmplitude = active.RecoveryJitterAmplitude;
        recovery.JitterFrequency = active.RecoveryJitterFrequency;
        recovery.JitterRefreshTime = active.RecoveryJitterRefreshTime;
        recovery.PopupInterval = active.RecoveryPopupInterval;
        recovery.Popups = new(active.RecoveryPopups);
        recovery.LastPopupIndex = -1;
        recovery.PopupBag.Clear();

        Dirty(uid, recovery);
    }

    private void ApplyJitter(EntityUid uid, ActiveSandevistanComponent active, float frameTime)
    {
        var lerp = Math.Clamp(frameTime * active.JitterLerpRate, 0f, 1f);
        active.JitterCurrentProgress = MathHelper.Lerp(active.JitterCurrentProgress, active.JitterTargetProgress, lerp);

        var amplitude = active.MaxJitterAmplitude * active.JitterCurrentProgress;
        var frequency = active.MaxJitterFrequency * active.JitterCurrentProgress;

        _jittering.DoJitter(
            uid,
            TimeSpan.FromSeconds(MathF.Max(active.JitterRefreshTime, 0.1f)),
            true,
            amplitude,
            frequency,
            true);

        if (TryComp<JitteringComponent>(uid, out var jittering))
            Dirty(uid, jittering);
    }

    private void ApplyOverloadTicks(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        var interval = GetInterval(active.OverloadInterval);
        while (curTime >= active.NextOverloadTime)
        {
            ApplyOverload(uid, active);

            active.NextOverloadTime += interval;
        }
    }

    private void ApplyOverload(EntityUid uid, ActiveSandevistanComponent active)
    {
        if (TryComp<DamageableComponent>(uid, out var damageable))
        {
            _damageable.TryChangeDamage(
                (uid, damageable),
                active.OverloadDamage,
                ignoreResistances: true,
                interruptsDoAfters: false,
                origin: uid,
                ignoreGlobalModifiers: true);
        }
    }

    private void ApplyCompletionStaminaDamage(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        if (active.ExhaustionStaminaDamage <= 0f ||
            !TryComp<StaminaComponent>(uid, out var stamina))
            return;

        ForceStaminaCrit(uid, stamina, curTime);
    }

    private void ForceStaminaCrit(EntityUid uid, StaminaComponent stamina, TimeSpan curTime)
    {
        var shouldApplyStun = !stamina.Critical || !HasComp<StunnedComponent>(uid);
        stamina.Critical = true;
        stamina.StaminaDamage = stamina.CritThreshold;

        if (shouldApplyStun && _stun.TryUpdateParalyzeDuration(uid, stamina.StunTime))
            _stun.TrySeeingStars(uid);

        var nextUpdate = curTime + stamina.StunTime + ExhaustionStaminaCritBufferTime;
        if (stamina.NextUpdate < nextUpdate)
            stamina.NextUpdate = nextUpdate;

        EnsureComp<ActiveStaminaComponent>(uid);
        Dirty(uid, stamina);
    }

    private bool IsStaminaExhausted(EntityUid uid)
    {
        return TryComp<StaminaComponent>(uid, out var stamina) &&
            (stamina.Critical || _stamina.GetStaminaDamage(uid, stamina) >= stamina.CritThreshold);
    }

    private void ApplySoftcapFeedback(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        var endWarningActive = curTime >= active.EndTime - TimeSpan.FromSeconds(active.EndWarningLeadTime);
        if (!endWarningActive && curTime >= active.NextSoftcapPopupTime)
        {
            if (active.SoftcapPopups.Count > 0)
            {
                PopupFollowing(
                    uid,
                    Loc.GetString(PickFeedback(active.SoftcapPopups, active.SoftcapPopupBag, ref active.LastSoftcapPopupIndex)),
                    PopupType.LargeCaution);
            }

            active.NextSoftcapPopupTime = curTime + GetInterval(active.SoftcapPopupInterval);
        }

        if (curTime >= active.NextShoutTime)
        {
            if (active.SoftcapShouts.Count > 0)
            {
                _chat.TrySendInGameICMessage(
                    uid,
                    Loc.GetString(PickFeedback(active.SoftcapShouts, active.SoftcapShoutBag, ref active.LastSoftcapShoutIndex)),
                    InGameICChatType.Speak,
                    ChatTransmitRange.Normal,
                    checkRadioPrefix: false,
                    ignoreActionBlocker: true);
            }

            active.NextShoutTime = curTime + GetShoutInterval(active.ShoutMinInterval, active.ShoutMaxInterval);
        }
    }

    private void ApplyEndWarningFeedback(EntityUid uid, ActiveSandevistanComponent active, TimeSpan curTime)
    {
        if (active.EndWarningPopups.Count == 0 ||
            curTime < active.EndTime - TimeSpan.FromSeconds(active.EndWarningLeadTime) ||
            curTime < active.NextEndWarningTime)
        {
            return;
        }

        var count = Math.Max(active.EndWarningPopupCount, 1);
        for (var i = 0; i < count; i++)
        {
            PopupFollowing(
                uid,
                Loc.GetString(PickFeedback(active.EndWarningPopups, active.EndWarningPopupBag, ref active.LastEndWarningPopupIndex)),
                PopupType.LargeCaution);
        }

        active.NextEndWarningTime = curTime + GetInterval(active.EndWarningInterval);
    }

    private void UpdateRecovery(TimeSpan curTime)
    {
        var query = EntityQueryEnumerator<SandevistanRecoveryComponent>();
        while (query.MoveNext(out var uid, out var recovery))
        {
            if (Paused(uid))
                continue;

            if (curTime >= recovery.EndTime)
            {
                RemCompDeferred<SandevistanRecoveryComponent>(uid);
                continue;
            }

            ApplyRecoveryJitter(uid, recovery, curTime);

            if (curTime >= recovery.NextTickTime)
                ApplyRecoveryTick(uid, recovery, curTime);

            if (!IsCriticalOrDead(uid) && curTime >= recovery.NextPopupTime)
                ApplyRecoveryPopup(uid, recovery, curTime);
        }
    }

    private void ApplyRecoveryJitter(EntityUid uid, SandevistanRecoveryComponent recovery, TimeSpan curTime)
    {
        var remaining = MathF.Max(0f, (float) (recovery.EndTime - curTime).TotalSeconds);
        var progress = Math.Clamp(remaining / MathF.Max(recovery.Duration, 0.1f), 0f, 1f);

        _jittering.DoJitter(
            uid,
            TimeSpan.FromSeconds(MathF.Max(recovery.JitterRefreshTime, 0.1f)),
            true,
            recovery.JitterAmplitude * progress,
            recovery.JitterFrequency * progress,
            true);

        if (TryComp<JitteringComponent>(uid, out var jittering))
            Dirty(uid, jittering);
    }

    private void ApplyRecoveryTick(EntityUid uid, SandevistanRecoveryComponent recovery, TimeSpan curTime)
    {
        if (TryComp<DamageableComponent>(uid, out var damageable))
        {
            _damageable.TryChangeDamage(
                (uid, damageable),
                recovery.Damage,
                ignoreResistances: true,
                interruptsDoAfters: false,
                origin: uid,
                ignoreGlobalModifiers: true);
        }

        recovery.NextTickTime = curTime + GetInterval(recovery.TickInterval);
    }

    private void ApplyRecoveryPopup(EntityUid uid, SandevistanRecoveryComponent recovery, TimeSpan curTime)
    {
        if (recovery.Popups.Count > 0)
        {
            PopupFollowing(
                uid,
                Loc.GetString(PickFeedback(recovery.Popups, recovery.PopupBag, ref recovery.LastPopupIndex)),
                PopupType.LargeCaution);
        }

        recovery.NextPopupTime = curTime + GetInterval(recovery.PopupInterval);
    }

    private void UpdateVisualFadeouts(TimeSpan curTime)
    {
        var query = EntityQueryEnumerator<SandevistanVisualFadeoutComponent>();
        while (query.MoveNext(out var uid, out var fadeout))
        {
            if (Paused(uid))
                continue;

            if (curTime >= fadeout.EndTime)
                RemCompDeferred<SandevistanVisualFadeoutComponent>(uid);
        }
    }

    private static float GetActiveVisualIntensity(ActiveSandevistanComponent active, TimeSpan curTime)
    {
        var remaining = MathF.Max(0f, (float) (active.EndTime - curTime).TotalSeconds);
        return SmoothStep(Math.Clamp(remaining / VisualFadeDuration, 0f, 1f));
    }

    private static float GetActiveSoftcapProgress(ActiveSandevistanComponent active, TimeSpan curTime)
    {
        var rampStart = active.SoftcapTime - TimeSpan.FromSeconds(SoftcapRampLeadTime);
        if (curTime < rampStart)
            return 0f;

        var elapsed = MathF.Max(0f, (float) (curTime - rampStart).TotalSeconds);
        return SmoothStep(Math.Clamp(elapsed / SoftcapRampLeadTime, 0f, 1f));
    }

    private static float SmoothStep(float progress)
    {
        return progress * progress * (3f - 2f * progress);
    }

    private static TimeSpan GetInterval(float seconds)
    {
        return TimeSpan.FromSeconds(MathF.Max(seconds, 0.1f));
    }

    private TimeSpan GetShoutInterval(float minSeconds, float maxSeconds)
    {
        var min = MathF.Max(minSeconds, 0.1f);
        var max = MathF.Max(maxSeconds, min);
        return TimeSpan.FromSeconds(_random.NextFloat(min, max));
    }

    private LocId PickFeedback(List<LocId> locIds, List<int> bag, ref int lastIndex)
    {
        if (locIds.Count == 1)
        {
            bag.Clear();
            lastIndex = 0;
            return locIds[0];
        }

        bag.RemoveAll(index => index < 0 || index >= locIds.Count);

        if (bag.Count == 0)
        {
            for (var i = 0; i < locIds.Count; i++)
            {
                if (i == lastIndex)
                    continue;

                bag.Add(i);
            }
        }

        var bagIndex = _random.Next(bag.Count);
        var locIndex = bag[bagIndex];
        bag.RemoveAt(bagIndex);
        lastIndex = locIndex;
        return locIds[locIndex];
    }

    private void PopupFollowing(EntityUid uid, string message, PopupType type)
    {
        _popup.PopupCoordinates(message, new EntityCoordinates(uid, 0, 0), uid, type);
    }
}
