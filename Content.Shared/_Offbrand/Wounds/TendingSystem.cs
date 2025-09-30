using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Medical.Healing;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.StatusEffectNew;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Offbrand.Wounds;

public sealed class TendingSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TendingComponent, UseInHandEvent>(OnUseInHand, before: new[] { typeof(HealingSystem) });
        SubscribeLocalEvent<TendingComponent, AfterInteractEvent>(OnAfterInteract, before: new[] { typeof(HealingSystem) });
        SubscribeLocalEvent<TendableWoundComponent, TendingDoAfterEvent>(OnTendingDoAfter);
    }

    private void OnUseInHand(Entity<TendingComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryTend(ent, args.User, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<TendingComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryTend(ent, args.Target.Value, args.User))
            args.Handled = true;
    }

    private Entity<TendableWoundComponent, WoundComponent>? GetWoundToTend(Entity<TendingComponent> ent, Entity<WoundableComponent?> target)
    {
        if (!_statusEffects.TryEffectsWithComp<TendableWoundComponent>(target, out var effects))
        {
            return null;
        }

        foreach (var wound in effects)
        {
            if (wound.Comp1.Tended)
                continue;

            if (!_entityWhitelist.CheckBoth(wound, ent.Comp.WoundBlacklist, ent.Comp.WoundWhitelist))
                continue;

            return (wound.Owner, wound.Comp1, Comp<WoundComponent>(wound));
        }

        return null;

    }

    private bool TryTend(Entity<TendingComponent> ent, Entity<WoundableComponent?> target, EntityUid user, bool isRepeat = false)
    {
        if (!Resolve(target, ref target.Comp, false))
            return false;

        var woundToTend = GetWoundToTend(ent, target);
        if (woundToTend is not { } foundWound)
        {
            if (isRepeat)
                _popup.PopupClient(Loc.GetString(ent.Comp.NothingToTendRepeat, ("target", Identity.Entity(target, EntityManager)), ("tending", ent)), user);
            else
                _popup.PopupClient(Loc.GetString(ent.Comp.NothingToTend, ("target", Identity.Entity(target, EntityManager)), ("tending", ent)), user);

            return true;
        }

        if (user != target.Owner && !_interaction.InRangeUnobstructed(user, target.Owner, popup: true))
            return false;

        if (TryComp<StackComponent>(ent, out var stack) && stack.Count < 1)
            return false;

        _audio.PlayPredicted(ent.Comp.TendingBeginSound, ent, user);

        var differentTarget = user != target.Owner;

        var delay = ent.Comp.Delay;
        if (!differentTarget)
            delay *= ent.Comp.SelfTendPenaltyModifier;

        if (differentTarget)
        {
            _popup.PopupPredicted(
                Loc.GetString(ent.Comp.UserPopup, ("target", Identity.Entity(target, EntityManager)), ("tending", ent), ("wound", foundWound)),
                Loc.GetString(ent.Comp.OtherPopup, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("tending", ent), ("wound", foundWound)),
                target,
                user
            );
        }
        else
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.SelfPopup, ("tending", ent), ("wound", foundWound)), user);
        }

        var args =
            new DoAfterArgs(EntityManager, user, delay, new TendingDoAfterEvent(), foundWound, target: target, used: ent)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(args);
        return true;
    }

    private void OnTendingDoAfter(Entity<TendableWoundComponent> ent, ref TendingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        if (!TryComp<TendingComponent>(args.Used, out var tending))
            return;

        _woundable.TendWound(target, ent, tending.Damage);

        var hasMoreItems = true;
        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stack.Use(args.Used.Value, 1, stackComp);

            if (_stack.GetCount(args.Used.Value, stackComp) <= 0)
                hasMoreItems = false;
        }
        else
        {
            hasMoreItems = false;
            PredictedQueueDel(args.Used.Value);
        }

        _audio.PlayPredicted(tending.TendingEndSound, target, args.User);

        if (hasMoreItems)
        {
            TryTend((args.Used.Value, tending), target, args.Args.User, true);
        }
        else
        {
            _popup.PopupClient(Loc.GetString(tending.UsedUp, ("tending", args.Used.Value)), args.Args.User);
        }
    }
}
