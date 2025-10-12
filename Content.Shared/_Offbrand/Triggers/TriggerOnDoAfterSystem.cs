using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Trigger.Systems;
using Content.Shared.Trigger;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Offbrand.Triggers;

public sealed class TriggerOnDoAfterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnDoAfterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TriggerOnDoAfterComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TriggerOnDoAfterComponent, TriggerOnDoAfterDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(Entity<TriggerOnDoAfterComponent> trigger, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryTrigger(trigger, args.User, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<TriggerOnDoAfterComponent> trigger, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryTrigger(trigger, args.Target.Value, args.User))
            args.Handled = true;
    }

    private void OnDoAfter(Entity<TriggerOnDoAfterComponent> trigger, ref TriggerOnDoAfterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target || args.Used is not { } used)
            return;

        var successful = _trigger.Trigger(trigger, target, trigger.Comp.KeyOut);

        var user = args.User;
        var differentTarget = user != target;

        if (differentTarget)
        {
            if (trigger.Comp.UserCompleted is { } userCompleted && trigger.Comp.OtherCompleted is { } otherCompleted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(userCompleted, ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    Loc.GetString(otherCompleted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    target,
                    user
                );
            }
        }
        else
        {
            if (trigger.Comp.SelfUserCompleted is { } selfUserCompleted && trigger.Comp.SelfOtherCompleted is { } selfOtherCompleted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(selfUserCompleted, ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    Loc.GetString(selfOtherCompleted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    target,
                    user
                );
            }
        }

        var hasMoreItems = true;
        if (trigger.Comp.Consume)
        {
            if (TryComp<StackComponent>(used, out var stackComp))
            {
                _stack.Use(used, 1, stackComp);

                if (_stack.GetCount(used, stackComp) <= 0)
                    hasMoreItems = false;
            }
            else
            {
                hasMoreItems = false;
                PredictedQueueDel(used);
            }
        }

        _audio.PlayPredicted(trigger.Comp.EndSound, target, args.User);

        if (hasMoreItems)
        {
            if (!successful || !trigger.Comp.AttemptRepeat)
            {
                return;
            }
            var attemptTriggerEvent = new AttemptTriggerEvent(target, trigger.Comp.KeyOut);
            RaiseLocalEvent(trigger, ref attemptTriggerEvent);

            if (attemptTriggerEvent.Cancelled)
            {
                if (trigger.Comp.ConditionFailedRepeat is { } conditionFailedRepeat)
                    _popup.PopupClient(Loc.GetString(conditionFailedRepeat, ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)), args.User);
            }
            else
                args.Repeat = true;
        }
        else
        {
            if (trigger.Comp.ItemsUsedUp is { } usedUp)
                _popup.PopupClient(Loc.GetString(usedUp, ("trigger", args.Used.Value)), args.Args.User);
        }
    }

    private bool TryTrigger(Entity<TriggerOnDoAfterComponent> trigger, EntityUid target, EntityUid user)
    {
        if (!_entityWhitelist.CheckBoth(target, trigger.Comp.Blacklist, trigger.Comp.Whitelist))
            return false;

        var attemptTriggerEvent = new AttemptTriggerEvent(target, trigger.Comp.KeyOut);
        RaiseLocalEvent(trigger, ref attemptTriggerEvent);

        if (attemptTriggerEvent.Cancelled)
        {
            if (trigger.Comp.ConditionFailed is { } conditionFailed)
                _popup.PopupClient(Loc.GetString(conditionFailed, ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)), user);

            return true;
        }

        if (user != target && !_interaction.InRangeUnobstructed(user, target, popup: true))
            return false;

        if (TryComp<StackComponent>(trigger, out var stack) && stack.Count < 1)
            return false;

        if (trigger.Comp.BeginSound is { } beginSound)
            _audio.PlayPredicted(beginSound, trigger, user);

        var differentTarget = user != target;

        if (differentTarget)
        {
            if (trigger.Comp.UserStarted is { } userStarted && trigger.Comp.OtherStarted is { } otherStarted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(userStarted, ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    Loc.GetString(otherStarted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    target,
                    user
                );
            }
        }
        else
        {
            if (trigger.Comp.SelfUserStarted is { } selfUserStarted && trigger.Comp.SelfOtherStarted is { } selfOtherStarted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(selfUserStarted, ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    Loc.GetString(selfOtherStarted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("trigger", trigger)),
                    target,
                    user
                );
            }
        }

        var delay = trigger.Comp.Delay;
        if (!differentTarget)
            delay *= trigger.Comp.SelfUsePenaltyModifier;

        var args =
            new DoAfterArgs(EntityManager, user, delay, new TriggerOnDoAfterDoAfterEvent(), trigger, target: target, used: trigger)
            {
                NeedHand = trigger.Comp.Params.NeedHand,
                BreakOnHandChange = trigger.Comp.Params.BreakOnHandChange,
                BreakOnDropItem = trigger.Comp.Params.BreakOnDropItem,
                BreakOnMove = trigger.Comp.Params.BreakOnMove,
                BreakOnWeightlessMove = trigger.Comp.Params.BreakOnWeightlessMove,
                MovementThreshold = trigger.Comp.Params.MovementThreshold,
                DistanceThreshold = trigger.Comp.Params.DistanceThreshold,
                BreakOnDamage = trigger.Comp.Params.BreakOnDamage,
                DamageThreshold = trigger.Comp.Params.DamageThreshold,
                RequireCanInteract = trigger.Comp.Params.RequireCanInteract,
                BlockDuplicate = trigger.Comp.Params.BlockDuplicate,
                CancelDuplicate = trigger.Comp.Params.CancelDuplicate,
                DuplicateCondition = trigger.Comp.Params.DuplicateCondition,
            };

        _doAfter.TryStartDoAfter(args);
        return true;
    }
}
