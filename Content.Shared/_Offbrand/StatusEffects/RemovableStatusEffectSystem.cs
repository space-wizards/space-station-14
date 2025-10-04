using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Verbs;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class RemovableStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemovableStatusEffectComponent, StatusEffectRelayedEvent<GetVerbsEvent<AlternativeVerb>>>(OnGetVerbs);
        SubscribeLocalEvent<RemovableStatusEffectComponent, RemoveStatusEffectEvent>(OnRemoveStatusEffect);
    }

    private void OnGetVerbs(Entity<RemovableStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetVerbsEvent<AlternativeVerb>> args)
    {
        if (!args.Args.CanAccess || !args.Args.CanInteract)
            return;

        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        var @event = args.Args;
        args.Args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                RemoveStatusEffect(ent, target, @event.User);
            },
            Text = Loc.GetString(ent.Comp.Verb),
        });
    }

    private void RemoveStatusEffect(Entity<RemovableStatusEffectComponent> ent, EntityUid target, EntityUid user)
    {
        var differentTarget = user != target;

        if (differentTarget)
        {
            if (ent.Comp.UserStarted is { } userStarted && ent.Comp.OtherStarted is { } otherStarted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(userStarted, ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    Loc.GetString(otherStarted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    target,
                    user
                );
            }
        }
        else
        {
            if (ent.Comp.SelfUserStarted is { } selfUserStarted && ent.Comp.SelfOtherStarted is { } selfOtherStarted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(selfUserStarted, ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    Loc.GetString(selfOtherStarted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    target,
                    user
                );
            }
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.RemovalTime,
            new RemoveStatusEffectEvent(), eventTarget: ent, target: target)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnRemoveStatusEffect(Entity<RemovableStatusEffectComponent> ent, ref RemoveStatusEffectEvent args)
    {
        if (args.Cancelled)
            return;

        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        var user = args.User;
        var differentTarget = user != target;

        if (differentTarget)
        {
            if (ent.Comp.UserCompleted is { } userCompleted && ent.Comp.OtherCompleted is { } otherCompleted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(userCompleted, ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    Loc.GetString(otherCompleted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    target,
                    user
                );
            }
        }
        else
        {
            if (ent.Comp.SelfUserCompleted is { } selfUserCompleted && ent.Comp.SelfOtherCompleted is { } selfOtherCompleted)
            {
                _popup.PopupPredicted(
                    Loc.GetString(selfUserCompleted, ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    Loc.GetString(selfOtherCompleted, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("effect", ent)),
                    target,
                    user
                );
            }
        }

        if (ent.Comp.SpawnOnRemove is { } proto)
        {
            var uid = PredictedSpawnNextToOrDrop(proto, target);

            _hands.TryPickupAnyHand(args.User, uid);
        }

        PredictedQueueDel(ent.Owner);
    }
}
