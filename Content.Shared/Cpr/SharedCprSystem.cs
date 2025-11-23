using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

using Robust.Shared.Configuration;

namespace Content.Shared.Cpr;

/// <summary>
/// Used for handling CPR on critical breathing mobs
/// </summary>
public abstract partial class SharedCprSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly EntityManager Ent = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const float CprInteractionRangeMultiplier = 0.25f;
    public const float CprDoAfterDelay = 0.7f;
    public const float CprAnimationLength = 0.2f;
    public const float CprAnimationEndTime = 1f; // This is set to much higher than the actual animation length to avoid it stopping prematurely, as it did in testing. Shouldnt affect anything

    // This determines how long the effects of a CPR interaction last, and so how often it needs to be repeated.
    // Damage will automatically scale to the less frequent "hits", to retain the overall dps of resuscitation
    public const float CprManualEffectDuration = 5f;

    // The total time window for a "correctly timed" CPR interaction. Outside this, the player will be told they are too fast/slow.
    public const float CprManualThreshold = 1.5f;

    private bool _cprRepeat;

    public override void Initialize()
    {
        base.Initialize();

        _config.OnValueChanged(CCVars.CprRepeat, value => _cprRepeat = value, true);

        SubscribeLocalEvent<CprComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<CprComponent, CprDoAfterEvent>(OnCprDoAfter);
    }
    /// <summary>
    /// Returns true if the CPRComponent has not been given care long enough to allow a new caretaker
    /// </summary>
    /// <param name="cpr">The CPRComponent</param>
    public bool CprCaretakerOutdated(CprComponent cpr)
    {
        return Timing.CurTime.Seconds - cpr.LastTimeGivenCare.Seconds > CprDoAfterDelay;
    }

    /// <summary>
    /// Returns whether or not the giver can give CPR to the recipient, ignoring the range requirement
    /// </summary>
    /// <param name="recipient">The one receiving CPR</param>
    /// <param name="giver">The one giving CPR</param>
    public bool CanDoCpr(EntityUid recipient, EntityUid giver)
    {
        if (!TryComp<CprComponent>(recipient, out var cpr) ||
            !TryComp<MobThresholdsComponent>(recipient, out var thresholds) ||
            !TryComp<MobThresholdsComponent>(giver, out var myThresholds))
            return false;

        if (thresholds.CurrentThresholdState != MobState.Critical ||
            myThresholds.CurrentThresholdState == MobState.Critical ||
            myThresholds.CurrentThresholdState == MobState.Dead)
            return false;

        // return false if someone else has very recently given care already
        if (cpr.LastCaretaker.HasValue &&
            !CprCaretakerOutdated(cpr) &&
            cpr.LastCaretaker.Value != giver)
            return false;

        return true;
    }
    /// <summary>
    /// Returns whether or not the giver is in range to do CPR on the recipient
    /// </summary>
    /// <param name="recipient">The one receiving CPR</param>
    /// <param name="giver">The one giving CPR</param>
    public bool InRangeForCpr(EntityUid recipient, EntityUid giver)
    {
        return _interactionSystem.InRangeUnobstructed(giver, recipient, SharedInteractionSystem.InteractionRange * CprInteractionRangeMultiplier);
    }

    /// <summary>
    /// Function calld when a CPR Do-after finishes; does the pumping things
    /// </summary>
    /// <param name="ent">The recipient</param>
    /// <param name="args">DoAfter arguments</param>
    public void OnCprDoAfter(Entity<CprComponent> ent, ref CprDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!CanDoCpr(ent, args.User))
            return;

        if (!TryComp<DamageableComponent>(ent, out var damage) ||
            !TryComp<CprComponent>(ent, out var cpr) ||
            !TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return;

        DoLunge(args.User);

        _audio.PlayPredicted(cpr.Sound, ent.Owner, args.User);

        // If CPR is set to manual, multiply the damage done by how much slower the CPR attempts are expected to be performed
        var scaledDamage = _cprRepeat
            ? cpr.Change
            : cpr.Change * ((CprManualEffectDuration - CprManualThreshold) / CprDoAfterDelay);

        _damage.TryChangeDamage((ent, damage), scaledDamage, interruptsDoAfters: false, ignoreResistances: true);

        // assist respiration of the target
        var assist = EnsureComp<AssistedRespirationComponent>(ent);

        // Determine how long the CPR's effect will last, depending on whether it's autorepeat or manual
        // This is to prevent people from optimising their game into suffering, by manually doing "slow" CPR when it's set to repeat
        var newUntil = _cprRepeat
            ? Timing.CurTime + TimeSpan.FromSeconds(CprDoAfterDelay + 0.25f)
            :  Timing.CurTime + TimeSpan.FromSeconds(CprManualEffectDuration);
        // comparing just in case other future sources may provide a longer timeframe of assisted respiration
        if (newUntil > assist.AssistedUntil)
            assist.AssistedUntil = newUntil;

        // burst of oxygen when not critical anymore
        if (thresholds.CurrentThresholdState != MobState.Critical)
        {
            _damage.TryChangeDamage((ent, damage), cpr.BonusHeal, interruptsDoAfters: false, ignoreResistances: true);
        }

        // set the cpr's latest caretaker and time
        cpr.LastCaretaker = args.User;
        cpr.LastTimeGivenCare = Timing.CurTime;

        args.Repeat = thresholds.CurrentThresholdState == MobState.Critical && _cprRepeat;
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
    public void TryStartCpr(EntityUid recipient, EntityUid giver)
    {
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            giver,
            TimeSpan.FromSeconds(CprDoAfterDelay),
            new CprDoAfterEvent(),
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
        if (!CanDoCpr(recipient, giver)
            || !InRangeForCpr(recipient, giver)
            || !_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        var timeLeft = TimeSpan.Zero;
        if (TryComp<AssistedRespirationComponent>(recipient, out var comp))
            timeLeft = comp.AssistedUntil - Timing.CurTime;

        // The recommended wait between compressions, leaving room for imperfect timing
        var recommendedRate = Math.Round(CprManualEffectDuration - CprManualThreshold);
        // A new CPR attempt is starting
        if (comp is null)
        {
            var localString = Loc.GetString("cpr-start-you", ("target", Identity.Entity(recipient, EntityManager)));
            var othersString = Loc.GetString("cpr-start", ("person", Identity.Entity(giver, EntityManager)), ("target", Identity.Entity(recipient, EntityManager)));
            _popup.PopupPredicted(localString, othersString, giver, giver, PopupType.Medium); //TODO:ERRANT only shows for others
        }
        // If the last CPR attempt came too late (eventually leading to gasping), warn the player
        else if (!_cprRepeat && timeLeft <= TimeSpan.Zero)
        {
            _popup.PopupCursor(Loc.GetString("cpr-too-slow", ("seconds", recommendedRate)), giver, PopupType.Large); //TODO:ERRANT why does only popupcursor work??
        }
        // If the CPR attempt came too soon (causing unnecessary blunt damage over time due to extra compressions), warn the player
        else if (timeLeft > TimeSpan.FromSeconds(CprManualEffectDuration - CprManualThreshold))
        {
            _popup.PopupCursor(Loc.GetString("cpr-too-fast", ("seconds", recommendedRate)), giver, PopupType.Large); //TODO:ERRANT why does only popupcursor work??
        }
    }

    private void OnGetAlternativeVerbs(EntityUid uid, CprComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!CanDoCpr(uid, args.User))
            return;

        var inRange = InRangeForCpr(uid, args.User);

        var verb = new AlternativeVerb()
        {
            Act = () =>
            {
                TryStartCpr(uid, args.User);
            },
            Text = Loc.GetString("cpr-verb-text"),
            Priority = 5,
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
public sealed partial class CprDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
