﻿using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Buckle;

// Partial class containing interaction & verb event handlers
public abstract partial class SharedBuckleSystem
{
    private void InitializeInteraction()
    {
        SubscribeLocalEvent<StrapComponent, GetVerbsEvent<InteractionVerb>>(AddStrapVerbs);
        SubscribeLocalEvent<StrapComponent, InteractHandEvent>(OnStrapInteractHand, after: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<StrapComponent, DragDropTargetEvent>(OnStrapDragDropTarget);
        SubscribeLocalEvent<StrapComponent, CanDropTargetEvent>(OnCanDropTarget);

        SubscribeLocalEvent<BuckleComponent, InteractHandEvent>(OnBuckleInteractHand, after: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<BuckleComponent, GetVerbsEvent<InteractionVerb>>(AddUnbuckleVerb);
    }

    private void OnCanDropTarget(EntityUid uid, StrapComponent component, ref CanDropTargetEvent args)
    {
        args.CanDrop = StrapCanDragDropOn(uid, args.User, uid, args.Dragged, component);
        args.Handled = true;
    }

    private void OnStrapDragDropTarget(EntityUid uid, StrapComponent component, ref DragDropTargetEvent args)
    {
        if (!StrapCanDragDropOn(uid, args.User, uid, args.Dragged, component))
            return;

        args.Handled = TryBuckle(args.Dragged, args.User, uid, popup: false);
    }

    private bool StrapCanDragDropOn(
        EntityUid strapUid,
        EntityUid userUid,
        EntityUid targetUid,
        EntityUid buckleUid,
        StrapComponent? strapComp = null,
        BuckleComponent? buckleComp = null)
    {
        if (!Resolve(strapUid, ref strapComp, false) ||
            !Resolve(buckleUid, ref buckleComp, false))
        {
            return false;
        }

        bool Ignored(EntityUid entity) => entity == userUid || entity == buckleUid || entity == targetUid;

        return _interaction.InRangeUnobstructed(targetUid, buckleUid, buckleComp.Range, predicate: Ignored);
    }

    private void OnStrapInteractHand(EntityUid uid, StrapComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Enabled)
            return;

        if (!TryComp(args.User, out BuckleComponent? buckle))
            return;

        if (buckle.BuckledTo == null)
            TryBuckle(args.User, args.User, uid, buckle, popup: true);
        else if (buckle.BuckledTo == uid)
            TryUnbuckle(args.User, args.User, buckle, popup: true);
        else
            return;

        // TODO BUCKLE add out bool for whether a pop-up was generated or not.
        args.Handled = true;
    }

    private void OnBuckleInteractHand(Entity<BuckleComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.BuckledTo != null)
            TryUnbuckle(ent!, args.User, popup: true);

        // TODO BUCKLE add out bool for whether a pop-up was generated or not.
        args.Handled = true;
    }

    private void AddStrapVerbs(EntityUid uid, StrapComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract || !component.Enabled)
            return;

        // Note that for whatever bloody reason, buckle component has its own interaction range. Additionally, this
        // range can be set per-component, so we have to check a modified InRangeUnobstructed for every verb.

        // Add unstrap verbs for every strapped entity.
        foreach (var entity in component.BuckledEntities)
        {
            var buckledComp = Comp<BuckleComponent>(entity);

            if (!_interaction.InRangeUnobstructed(args.User, args.Target, range: buckledComp.Range))
                continue;

            var verb = new InteractionVerb()
            {
                Act = () => TryUnbuckle(entity, args.User, buckleComp: buckledComp),
                Category = VerbCategory.Unbuckle,
                Text = entity == args.User
                    ? Loc.GetString("verb-self-target-pronoun")
                    : Identity.Name(entity, EntityManager)
            };

            // In the event that you have more than once entity with the same name strapped to the same object,
            // these two verbs will be identical according to Verb.CompareTo, and only one with actually be added to
            // the verb list. However this should rarely ever be a problem. If it ever is, it could be fixed by
            // appending an integer to verb.Text to distinguish the verbs.

            args.Verbs.Add(verb);
        }

        // Add a verb to buckle the user.
        if (TryComp<BuckleComponent>(args.User, out var buckle) &&
            buckle.BuckledTo != uid &&
            args.User != uid &&
            StrapHasSpace(uid, buckle, component) &&
            _interaction.InRangeUnobstructed(args.User, args.Target, range: buckle.Range))
        {
            InteractionVerb verb = new()
            {
                Act = () => TryBuckle(args.User, args.User, args.Target, buckle),
                Category = VerbCategory.Buckle,
                Text = Loc.GetString("verb-self-target-pronoun")
            };
            args.Verbs.Add(verb);
        }

        // If the user is currently holding/pulling an entity that can be buckled, add a verb for that.
        if (args.Using is { Valid: true } @using &&
            TryComp<BuckleComponent>(@using, out var usingBuckle) &&
            StrapHasSpace(uid, usingBuckle, component) &&
            _interaction.InRangeUnobstructed(@using, args.Target, range: usingBuckle.Range))
        {
            // Check that the entity is unobstructed from the target (ignoring the user).
            bool Ignored(EntityUid entity) => entity == args.User || entity == args.Target || entity == @using;
            if (!_interaction.InRangeUnobstructed(@using, args.Target, usingBuckle.Range, predicate: Ignored))
                return;

            var isPlayer = _playerManager.TryGetSessionByEntity(@using, out var _);
            InteractionVerb verb = new()
            {
                Act = () => TryBuckle(@using, args.User, args.Target, usingBuckle),
                Category = VerbCategory.Buckle,
                Text = Identity.Name(@using, EntityManager),
                // just a held object, the user is probably just trying to sit down.
                // If the used entity is a person being pulled, prioritize this verb. Conversely, if it is
                Priority = isPlayer ? 1 : -1
            };

            args.Verbs.Add(verb);
        }
    }

    private void AddUnbuckleVerb(EntityUid uid, BuckleComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !component.Buckled)
            return;

        InteractionVerb verb = new()
        {
            Act = () => TryUnbuckle(uid, args.User, buckleComp: component),
            Text = Loc.GetString("verb-categories-unbuckle"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png"))
        };

        if (args.Target == args.User && args.Using == null)
        {
            // A user is left clicking themselves with an empty hand, while buckled.
            // It is very likely they are trying to unbuckle themselves.
            verb.Priority = 1;
        }

        args.Verbs.Add(verb);
    }

}
