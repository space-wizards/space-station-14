using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Medical.Breathalyzer.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Medical.Breathalyzer;

public sealed class BreathalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Damage type to check if the target is capable of breathing into a breathalyzer, since RespiratorSystem is server-only
    /// </summary>
    [ValidatePrototypeId<DamageTypePrototype>]
    private const string BreathDamageType = "Asphyxiation";

    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string DrunkKey = "Drunk";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BreathalyzerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<BreathalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BreathalyzerComponent, BreathalyzerActionEvent>(OnBreathalyzerAction);
        SubscribeLocalEvent<BreathalyzerComponent, BreathalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BreathalyzerComponent, ExaminedEvent>(OnExamined);
    }

    #region Event handlers
    private void OnUtilityVerb(EntityUid uid, BreathalyzerComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                StartChecking((uid, component), args.Target);
            },
            Text = Loc.GetString("breathalyzer-verb-text"),
            Message = Loc.GetString("breathalyzer-verb-message", ("target", Identity.Entity(args.Target, EntityManager)))
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(Entity<BreathalyzerComponent> entity, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target || args.Handled)
            return;

        args.Handled = StartChecking(entity, target);
    }

    /// <summary>
    /// Starts repeatedly checking <see cref="args.Target"/>'s drunkenness
    /// </summary>
    private void OnBreathalyzerAction(Entity<BreathalyzerComponent> ent, ref BreathalyzerActionEvent args)
    {
        StartChecking(ent, args.Target);
    }

    /// <summary>
    /// Checks <see cref="args.Target"/>'s drunkenness once.<br/>
    /// Gets called repeatedly after <see cref="StartChecking"/> is called
    /// </summary>
    private void OnDoAfter(Entity<BreathalyzerComponent> ent, ref BreathalyzerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Repeat = true;
        args.Handled = true;
        ExamineWithBreathalyzer(ent, args.Args.User, target);
    }

    private void OnExamined(Entity<BreathalyzerComponent> ent, ref ExaminedEvent args)
    {
        var msg = new FormattedMessage();
        if (ent.Comp.LastReadValue is { } lastReadValue)
            msg.AddMarkupPermissive(Loc.GetString("breathalyzer-last-read", ("lastReadValue", FormatRemainingTimeSpan(lastReadValue))));
        else
            msg.AddMarkupPermissive(Loc.GetString("breathalyzer-no-last-read"));

        args.PushMessage(msg, 1);
    }
    #endregion

    /// <summary>
    /// Fire off the DoAfter for repeatedly checking <see cref="target"/>'s drunkenness
    /// </summary>
    private bool StartChecking(Entity<BreathalyzerComponent> ent, EntityUid target)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container))
        {
            ent.Comp.LastReadValue = null;
            return false;
        }

        var user = container.Owner;
        if (!TryGetDrunkenness(user, target, out _))
        {
            ent.Comp.LastReadValue = null;
            return false;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.Delay, new BreathalyzerDoAfterEvent(), ent, target, ent)
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
            Hidden = true,
            BreakOnHandChange = true,
            NeedHand = true,
            BreakOnDamage = true, // Give the angry drunks a chance to resist :⁾
        });
        return true;
    }

    /// <summary>
    /// Tries to get <see cref="target"/>'s drunkenness.
    /// </summary>
    /// <param name="target">The <see cref="EntityUid"/> to check the drunkenness of</param>
    /// <param name="boozeTime">The remaining time in seconds that the target will be drunk for, <see langword="null"/> if getting the value failed, <c>0</c> if not drunk.</param>
    /// <returns>Whether <see cref="target"/> is capable of getting their drunkenness checked</returns>
    private bool TryGetDrunkenness(EntityUid user, EntityUid target, [NotNullWhen(true)] out double? boozeTime)
    {
        // TODO: Add check for respirator component when it gets moved to shared.
        // Check if the mob is dead and can take asphyxiation damage, else abort here.
        if (!TryComp<MobStateComponent>(target, out var mobState) ||
            !TryComp<DamageableComponent>(target, out var damageComp) ||
            _mobState.IsDead(target, mobState) ||
            !damageComp.Damage.DamageDict.ContainsKey(BreathDamageType))
        {
            _popup.PopupPredicted(Loc.GetString("breathalyzer-cannot-breathe"), target, user);
            boozeTime = null;
            return false;
        }

        if (!_statusEffects.TryGetTime(target, DrunkKey, out var boozeTimeNullable))
        {
            boozeTime = 0;
            return true;
        }
        boozeTime = (boozeTimeNullable.Value.Item2 - boozeTimeNullable.Value.Item1).TotalSeconds;
        return true;
    }

    /// <summary>
    /// Calculates the approximate drunkenness value and shows it to the user
    /// </summary>
    private void ExamineWithBreathalyzer(Entity<BreathalyzerComponent> breathalyzer, EntityUid user, EntityUid target)
    {
        if (!TryGetDrunkenness(user, target, out var maybeBoozeTime) || maybeBoozeTime is not { } boozeTime)
            return;

        // Praying for https://github.com/space-wizards/RobustToolbox/pull/5849 to make this usable
        // Get remaining time of drunkenness, offset with random value based on accuracy
        // var drunkenness = _random.NextGaussian(boozeTime, breathalyzer.Comp.Variance);
        var drunkenness = boozeTime;

        // Round to closest multiple of Specificity
        var approximateDrunkenness = (ulong)(Math.Round(drunkenness / breathalyzer.Comp.Specificity) * breathalyzer.Comp.Specificity);

        LocId chosenLocId = "breathalyzer-sober";
        // Go through all reached thresholds and pick the last one.
        foreach (var (threshold, locId) in breathalyzer.Comp.Thresholds)
        {
            if (approximateDrunkenness >= threshold)
                chosenLocId = locId;
            else
                break;
        }
        var readValue = TimeSpan.FromSeconds(approximateDrunkenness);
        breathalyzer.Comp.LastReadValue = readValue;
        _popup.PopupPredicted(
            Loc.GetString(
                chosenLocId,
                ("approximateDrunkenness", FormatRemainingTimeSpan(readValue))
            ),
            target,
            user
        );
    }

    private static string FormatRemainingTimeSpan(TimeSpan time)
    {
        return time switch
        {
            { TotalDays: >= 1 } => time.ToString(@"d\dhh\hmm\mss\s"),
            { TotalHours: >= 1 } => time.ToString(@"h\hmm\mss\s"),
            { TotalMinutes: >= 1 } => time.ToString(@"m\mss\s"),
            _ => time.ToString(@"s\s"),
        };
    }
}

public sealed partial class BreathalyzerActionEvent : EntityTargetActionEvent;
