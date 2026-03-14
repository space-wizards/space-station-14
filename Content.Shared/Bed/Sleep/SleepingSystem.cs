using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Damage.Systems;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Slippery;
using Content.Shared.Sound;
using Content.Shared.Sound.Components;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Bed.Sleep;

public sealed partial class SleepingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEmitSoundSystem _emitSound = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public static readonly EntProtoId SleepActionId = "ActionSleep";
    public static readonly EntProtoId WakeActionId = "ActionWake";
    public static readonly EntProtoId StatusEffectForcedSleeping = "StatusEffectForcedSleeping";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsContainerComponent, SleepActionEvent>(OnBedSleepAction);

        SubscribeLocalEvent<MobStateComponent, SleepStateChangedEvent>(OnSleepStateChanged);
        SubscribeLocalEvent<MobStateComponent, WakeActionEvent>(OnWakeAction);
        SubscribeLocalEvent<MobStateComponent, SleepActionEvent>(OnSleepAction);

        SubscribeLocalEvent<SleepingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<SleepingComponent, EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<SleepingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SleepingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SleepingComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<SleepingComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<SleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<SleepingComponent, CanSeeAttemptEvent>(OnSeeAttempt);
        SubscribeLocalEvent<SleepingComponent, PointAttemptEvent>(OnPointAttempt);
        SubscribeLocalEvent<SleepingComponent, SlipAttemptEvent>(OnSlip);
        SubscribeLocalEvent<SleepingComponent, ConsciousAttemptEvent>(OnConsciousAttempt);
        SubscribeLocalEvent<SleepingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SleepingComponent, GetVerbsEvent<AlternativeVerb>>(AddWakeVerb);
        SubscribeLocalEvent<SleepingComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SleepingComponent, StunEndAttemptEvent>(OnStunEndAttempt);
        SubscribeLocalEvent<SleepingComponent, StandUpAttemptEvent>(OnStandUpAttempt);

        SubscribeLocalEvent<ForcedSleepingStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<SleepingComponent, UnbuckleAttemptEvent>(OnUnbuckleAttempt);
        SubscribeLocalEvent<SleepingComponent, EmoteAttemptEvent>(OnEmoteAttempt);

        SubscribeLocalEvent<SleepingComponent, BeforeForceSayEvent>(OnChangeForceSay, after: new []{typeof(PainNumbnessSystem)});
    }

    private void OnUnbuckleAttempt(Entity<SleepingComponent> ent, ref UnbuckleAttemptEvent args)
    {
        // TODO is this necessary?
        // Shouldn't the interaction have already been blocked by a general interaction check?
        if (ent.Owner == args.User)
            args.Cancelled = true;
    }

    private void OnBedSleepAction(Entity<ActionsContainerComponent> ent, ref SleepActionEvent args)
    {
        TrySleeping(args.Performer);
    }

    private void OnWakeAction(Entity<MobStateComponent> ent, ref WakeActionEvent args)
    {
        if (TryWakeWithCooldown(ent.Owner))
            args.Handled = true;
    }

    private void OnSleepAction(Entity<MobStateComponent> ent, ref SleepActionEvent args)
    {
        TrySleeping((ent, ent.Comp));
    }

    private void OnRejuvenate(Entity<SleepingComponent> ent, ref RejuvenateEvent args)
    {
        // WAKE UP!!!
        RemComp<SleepingComponent>(ent);
    }

    /// <summary>
    /// when sleeping component is added or removed, we do some stuff with other components.
    /// </summary>
    private void OnSleepStateChanged(Entity<MobStateComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
        {
            // Just in case we're not using the sleeping status
            EnsureComp<StunnedComponent>(ent);
            EnsureComp<KnockedDownComponent>(ent);

            if (TryComp<SleepEmitSoundComponent>(ent, out var sleepSound))
            {
                var emitSound = EnsureComp<SpamEmitSoundComponent>(ent);
                if (HasComp<SnoringComponent>(ent))
                {
                    emitSound.Sound = sleepSound.Snore;
                }
                emitSound.MinInterval = sleepSound.Interval;
                emitSound.MaxInterval = sleepSound.MaxInterval;
                emitSound.PopUp = sleepSound.PopUp;
                Dirty(ent.Owner, emitSound);
            }

            return;
        }

        _stun.TryUnstun(ent.Owner);
        _stun.TryStanding(ent.Owner);

        RemComp<SpamEmitSoundComponent>(ent);
    }

    private void OnCompInit(Entity<SleepingComponent> ent, ref ComponentInit args)
    {
        var ev = new SleepStateChangedEvent(true);
        RaiseLocalEvent(ent, ref ev);
        _blindableSystem.UpdateIsBlind(ent.Owner);
        _actionsSystem.AddAction(ent, ref ent.Comp.WakeAction, WakeActionId, ent);
    }

    private void OnComponentRemoved(Entity<SleepingComponent> ent, ref ComponentRemove args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.WakeAction);

        var ev = new SleepStateChangedEvent(false);
        RaiseLocalEvent(ent, ref ev);

        _blindableSystem.UpdateIsBlind(ent.Owner);
    }

    private void OnSpeakAttempt(Entity<SleepingComponent> ent, ref SpeakAttemptEvent args)
    {
        // TODO reduce duplication of this behavior with MobStateSystem somehow
        if (HasComp<AllowNextCritSpeechComponent>(ent))
        {
            RemCompDeferred<AllowNextCritSpeechComponent>(ent);
            return;
        }

        args.Cancel();
    }

    private void OnSeeAttempt(Entity<SleepingComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnPointAttempt(Entity<SleepingComponent> ent, ref PointAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnSlip(Entity<SleepingComponent> ent, ref SlipAttemptEvent args)
    {
        args.NoSlip = true;
    }

    private void OnConsciousAttempt(Entity<SleepingComponent> ent, ref ConsciousAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStunEndAttempt(Entity<SleepingComponent> ent, ref StunEndAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStandUpAttempt(Entity<SleepingComponent> ent, ref StandUpAttemptEvent args)
    {
        // Shh the Urist McHands is sleeping...
        args.Cancelled = true;
    }

    private void OnExamined(Entity<SleepingComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushMarkup(Loc.GetString("sleep-examined", ("target", Identity.Entity(ent, EntityManager))));
        }
    }

    private void AddWakeVerb(Entity<SleepingComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryWakeWithCooldown((ent, ent.Comp), user: user);
            },
            Text = Loc.GetString("action-name-wake"),
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// When you click on a sleeping person with an empty hand, try to wake them.
    /// </summary>
    private void OnInteractHand(Entity<SleepingComponent> ent, ref InteractHandEvent args)
    {
        args.Handled = true;

        TryWakeWithCooldown((ent, ent.Comp), args.User);
    }

    /// <summary>
    /// Wake up on taking an instance of damage at least the value of WakeThreshold.
    /// </summary>
    private void OnDamageChanged(Entity<SleepingComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (args.DamageDelta.GetTotal() >= ent.Comp.WakeThreshold)
            TryWaking((ent, ent.Comp));
    }

    /// <summary>
    /// Wake up on being zombified.
    /// In some cases, zombification might theoretically occur without a mob state change or being damaged
    /// </summary>
    /// //TODO Perhaps a generic component should be introduced that guarantees that a mob will wake up immediately and can't go to sleep again
    private void OnZombified(Entity<SleepingComponent> ent, ref EntityZombifiedEvent args)
    {
        TryWaking((ent, ent.Comp), true);
    }

    /// <summary>
    /// In crit, we wake up if we are not being forced to sleep.
    /// And, you can't sleep when dead...
    /// </summary>
    private void OnMobStateChanged(Entity<SleepingComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<SpamEmitSoundComponent>(ent);
            RemComp<SleepingComponent>(ent);
            return;
        }
        if (TryComp<SpamEmitSoundComponent>(ent, out var spam))
            _emitSound.SetEnabled((ent, spam), args.NewMobState == MobState.Alive);
    }

    private void OnStatusEffectApplied(Entity<ForcedSleepingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        // Applying state check needed so we don't add SleepingComp during
        // entity reset due to the status effect getting inserted
        if (!_gameTiming.ApplyingState)
            TrySleeping(args.Target);
    }

    /// <summary>
    /// Try sleeping. Only mobs can sleep.
    /// </summary>
    public bool TrySleeping(Entity<MobStateComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        var tryingToSleepEvent = new TryingToSleepEvent(ent);
        RaiseLocalEvent(ent, ref tryingToSleepEvent);
        if (tryingToSleepEvent.Cancelled)
            return false;

        EnsureComp<SleepingComponent>(ent);
        return true;
    }

    /// <summary>
    /// Tries to wake up <paramref name="ent"/>, with a cooldown between attempts to prevent spam.
    /// </summary>
    public bool TryWakeWithCooldown(Entity<SleepingComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var curTime = _gameTiming.CurTime;

        if (curTime < ent.Comp.CooldownEnd)
            return false;

        ent.Comp.CooldownEnd = curTime + ent.Comp.Cooldown;
        Dirty(ent, ent.Comp);
        return TryWaking(ent, user: user);
    }

    /// <summary>
    /// Try to wake up <paramref name="ent"/>.
    /// </summary>
    public bool TryWaking(Entity<SleepingComponent?> ent, bool force = false, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!force && _statusEffect.HasEffectComp<ForcedSleepingStatusEffectComponent>(ent))
        {
            if (user != null)
            {
                _audio.PlayPredicted(ent.Comp.WakeAttemptSound, ent, user);
                _popupSystem.PopupClient(Loc.GetString("wake-other-failure", ("target", Identity.Entity(ent, EntityManager))), ent, user, PopupType.SmallCaution);
            }
            return false;
        }

        if (user != null)
        {
            _audio.PlayPredicted(ent.Comp.WakeAttemptSound, ent, user);
            _popupSystem.PopupClient(Loc.GetString("wake-other-success", ("target", Identity.Entity(ent, EntityManager))), ent, user);
        }

        return RemComp<SleepingComponent>(ent);
    }

    /// <summary>
    /// Prevents the use of emote actions while sleeping
    /// </summary>
    public void OnEmoteAttempt(Entity<SleepingComponent> ent, ref EmoteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnChangeForceSay(Entity<SleepingComponent> ent, ref BeforeForceSayEvent args)
    {
        args.Prefix = ent.Comp.ForceSaySleepDataset;
    }
}


public sealed partial class SleepActionEvent : InstantActionEvent;

public sealed partial class WakeActionEvent : InstantActionEvent;

/// <summary>
/// Raised on an entity when they fall asleep or wake up.
/// </summary>
[ByRefEvent]
public record struct SleepStateChangedEvent(bool FellAsleep);
