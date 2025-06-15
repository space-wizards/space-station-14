using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Net.Http.Headers;

namespace Content.Shared.CPR;
/// <summary>
/// Used for handling CPR on critical breathing mobs
/// </summary>
public abstract partial class SharedCPRSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly EntityManager Ent = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const float CPRInteractionRangeMultiplier = 0.25f;
    public const float CPRDoAfterDelay = 0.7f;
    public const float CPRAnimationLength = 0.2f;
    public const float CPRAnimationEndTime = 1f; // This is set to much higher than the actual animation length to avoid it stopping prematurely, as it did in testing. Shouldnt affect anything

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CPRComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<CPRComponent, CPRDoAfterEvent>(OnCPRDoAfter);
    }
    /// <summary>
    /// Returns true if the CPRComponent has not been given care long enough to allow a new caretaker
    /// </summary>
    /// <param name="cpr">The CPRComponent</param>
    public bool CPRCaretakerOutdated(CPRComponent cpr)
    {
        return Timing.CurTime.Seconds - cpr.LastTimeGivenCare.Seconds > CPRDoAfterDelay;
    }

    /// <summary>
    /// Returns whether or not the giver can give CPR to the recipient, ignoring the range requirement
    /// </summary>
    /// <param name="recipient">The one receiving CPR</param>
    /// <param name="giver">The one giving CPR</param>
    public bool CanDoCPR(EntityUid recipient, EntityUid giver)
    {
        if (!TryComp<CPRComponent>(recipient, out var cpr) ||
            !TryComp<MobThresholdsComponent>(recipient, out var thresholds) ||
            !TryComp<MobThresholdsComponent>(giver, out var myThresholds))
            return false;

        if (thresholds.CurrentThresholdState != MobState.Critical ||
            myThresholds.CurrentThresholdState == MobState.Critical ||
            myThresholds.CurrentThresholdState == MobState.Dead)
            return false;

        // return false if someone else has very recently given care already
        if (cpr.LastCaretaker.HasValue &&
            !CPRCaretakerOutdated(cpr) &&
            cpr.LastCaretaker.Value != giver)
            return false;

        return true;
    }
    /// <summary>
    /// Returns whether or not the giver is in range to do CPR on the recipient
    /// </summary>
    /// <param name="recipient">The one receiving CPR</param>
    /// <param name="giver">The one giving CPR</param>
    public bool InRangeForCPR(EntityUid recipient, EntityUid giver)
    {
        return _interactionSystem.InRangeUnobstructed(giver, recipient, SharedInteractionSystem.InteractionRange * CPRInteractionRangeMultiplier);
    }

    /// <summary>
    /// Function calld when a CPR Do-after finishes; does the pumping things
    /// </summary>
    /// <param name="ent">The recipient</param>
    /// <param name="args">DoAfter arguments</param>
    public void OnCPRDoAfter(Entity<CPRComponent> ent, ref CPRDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!CanDoCPR(ent, args.User))
            return;

        if (!TryComp<DamageableComponent>(ent, out var damage) ||
            !TryComp<CPRComponent>(ent, out var cpr) ||
            !TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return;

        DoLunge(args.User);

        _audio.PlayPredicted(cpr.Sound, ent.Owner, args.User);
        _damage.TryChangeDamage(ent, cpr.Change, interruptsDoAfters: false, damageable: damage, ignoreResistances: true);

        // assist respiration of the target
        var assist = EnsureComp<AssistedRespirationComponent>(ent);

        var newUntil = Timing.CurTime + TimeSpan.FromSeconds(CPRDoAfterDelay * 2);
        // comparing just in case other future sources may provide a longer timeframe of assisted respiration
        if (newUntil > assist.AssistedUntil)
            assist.AssistedUntil = newUntil;

        // burst of oxygen when not critical anymore
        if (thresholds.CurrentThresholdState != MobState.Critical)
        {
            _damage.TryChangeDamage(ent, cpr.BonusHeal, interruptsDoAfters: false, damageable: damage, ignoreResistances: true);
        }

        // set the cpr's latest caretaker and time
        cpr.LastCaretaker = args.User;
        cpr.LastTimeGivenCare = Timing.CurTime;

        args.Repeat = thresholds.CurrentThresholdState == MobState.Critical;
        args.Handled = true;
    }
    /// <summary>
    /// Makes a user do the CPR Lunge animation
    /// </summary>
    /// <param name="user">The entity to animate</param>
    public abstract void DoLunge(EntityUid user);

    /// <summary>
    /// Tries to start CPR between the giver and recipient, if possible
    /// </summary>
    /// <param name="recipient">The one receiving CPR</param>
    /// <param name="giver">The one giving CPR</param>
    public void TryStartCPR(EntityUid recipient, EntityUid giver)
    {
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            giver,
            TimeSpan.FromSeconds(CPRDoAfterDelay),
            new CPRDoAfterEvent(),
            recipient,
            giver
            )
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            RequireCanInteract = true,
            NeedHand = true
        };

        // try starting CPR
        if (CanDoCPR(recipient, giver) && InRangeForCPR(recipient, giver) && _doAfter.TryStartDoAfter(doAfterEventArgs))
        {
            var localString = Loc.GetString("cpr-start-you", ("target", Identity.Entity(recipient, EntityManager)));
            var othersString = Loc.GetString("cpr-start", ("person", Identity.Entity(giver, EntityManager)), ("target", Identity.Entity(recipient, EntityManager)));
            _popup.PopupPredicted(localString, othersString, giver, giver, PopupType.Medium);
        }
    }

    private void OnGetAlternativeVerbs(EntityUid uid, CPRComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!CanDoCPR(uid, args.User))
            return;

        var inRange = InRangeForCPR(uid, args.User);

        var verb = new AlternativeVerb()
        {
            Act = () =>
            {
                TryStartCPR(uid, args.User);
            },
            Text = Loc.GetString("cpr-verb-text"),
            Disabled = !inRange,
            Message = inRange ? null : Loc.GetString("cpr-verb-text-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cpr.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }
}

/// <summary>
/// Do-after event used for CPR
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
