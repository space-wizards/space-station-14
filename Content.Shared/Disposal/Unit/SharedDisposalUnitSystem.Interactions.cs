using Content.Shared.Body.Components;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Disposal.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Disposal.Unit;

public abstract partial class SharedDisposalUnitSystem
{
    #region: Event handling

    private void AddAltVerbs(Entity<DisposalUnitComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Behavior for if the disposals bin has items in it
        if (GetContainedEntityCount(ent) > 0)
        {
            // Verbs to flush the unit
            AlternativeVerb flushVerb = new()
            {
                Act = () => SetEngage(ent, true),
                Text = Loc.GetString("disposal-flush-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Priority = 1,
            };
            args.Verbs.Add(flushVerb);

            // Verb to eject the contents
            AlternativeVerb ejectVerb = new()
            {
                Act = () => EjectContents(ent),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("disposal-eject-verb-get-data-text")
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void AddInteractionVerb(Entity<DisposalUnitComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == null)
            return;

        if (!_actionBlocker.CanDrop(args.User))
            return;

        if (ent.Comp.Container == null || !_container.CanInsert(args.Using.Value, ent.Comp.Container))
            return;

        var verbData = args;

        InteractionVerb insertVerb = new()
        {
            Text = Name(args.Using.Value),
            Category = VerbCategory.Insert,
            Act = () =>
            {
                _hands.TryDropIntoContainer((verbData.User, verbData.Hands), verbData.Using.Value, ent.Comp.Container, checkActionBlocker: false);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(verbData.User):player} inserted {ToPrettyString(verbData.Using.Value)} into {ToPrettyString(ent)}");
                Insert(ent, verbData.Using.Value, verbData.User);
            }
        };

        args.Verbs.Add(insertVerb);
    }

    private void AddEnterOrExitVerb(Entity<DisposalUnitComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (ent.Comp.Container == null)
            return;

        // This is not an interaction, activation, or alternative verb type because unfortunately most users are
        // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
        if (!args.CanAccess ||
            !args.CanInteract ||
            !_actionBlocker.CanMove(args.User))
        {
            return;
        }

        var verbData = args;
        var verb = new Verb()
        {
            DoContactInteraction = true
        };

        if (!GetContainedEntities(ent).Contains(args.User))
        {
            if (!_container.CanInsert(args.User, ent.Comp.Container))
                return;

            // Verb for climbing in
            verb.Act = () => TryInsert(ent, verbData.User, verbData.User);
            verb.Text = Loc.GetString("verb-common-enter");
            verb.Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            // Verb for climbing out
            verb.Act = () => Remove(ent, verbData.User);
            verb.Text = Loc.GetString("verb-common-exit");
            verb.Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }

        args.Verbs.Add(verb);
    }

    private void OnDoAfter(Entity<DisposalUnitComponent> ent, ref DisposalDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        Insert(ent, args.Args.Target.Value, args.Args.User);

        args.Handled = true;
    }

    private void OnThrowInsert(Entity<DisposalUnitComponent> ent, ref BeforeThrowInsertEvent args)
    {
        if (ent.Comp.Container == null || !_container.CanInsert(args.ThrownEntity, ent.Comp.Container))
        {
            args.Cancelled = true;
        }
    }

    private void OnInsertAttempt(Entity<DisposalUnitComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container != ent.Comp.Container)
            return;

        if (GetContainedEntityCount(ent) >= ent.Comp.MaxCapacity)
        {
            // TODO: If ContainerIsInsertingAttemptEvent ever ends up having the user
            // attached to the event, we'll be able to predict the pop up
            _popup.PopupPredicted(Loc.GetString("disposal-unit-is-full"), ent, null);

            args.Cancel();
            return;
        }

        if (!Transform(ent).Anchored)
        {
            args.Cancel();
            return;
        }

        if (_whitelist.IsBlacklistPass(ent.Comp.Blacklist, args.EntityUid) ||
            _whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.EntityUid))
        {
            args.Cancel();
            return;
        }
    }

    private void OnActivate(Entity<DisposalUnitComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        _ui.TryToggleUi(ent.Owner, DisposalUnitUiKey.Key, args.User);
    }

    private void OnAfterInteractUsing(Entity<DisposalUnitComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (ent.Comp.Container == null ||
            !_container.CanInsert(args.Used, ent.Comp.Container) ||
            !_hands.TryDropIntoContainer(args.User, args.Used, ent.Comp.Container))
        {
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Used)} into {ToPrettyString(ent)}");
        Insert(ent, args.Used, args.User);
        args.Handled = true;
    }

    // TODO: This should just use the same thing as entity storage?
    private void OnMovement(Entity<DisposalUnitComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        var currentTime = _timing.CurTime;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        if (!TryComp(args.Entity, out HandsComponent? hands) ||
            hands.Count == 0 ||
            currentTime < ent.Comp.LastExitAttempt + ent.Comp.ExitAttemptDelay)
            return;

        Remove(ent, args.Entity);
        ent.Comp.LastExitAttempt = currentTime;

        UpdateUI(ent);
        Dirty(ent);
    }

    protected void OnCanDragDropOn(Entity<DisposalUnitComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Container == null)
            return;

        args.CanDrop = _container.CanInsert(args.Dragged, ent.Comp.Container);
        args.Handled = true;
    }

    private void OnDragDropOn(Entity<DisposalUnitComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryInsert(ent, args.Dragged, args.User);
    }

    #endregion

    /// <summary>
    /// Have a user try to insert an entity into a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="toInsert">The entity to insert.</param>
    /// <param name="user">The user inserting the entity.</param>
    /// <returns>True if the entity can be inserted.</returns>
    public bool TryInsert(Entity<DisposalUnitComponent> ent, EntityUid toInsert, EntityUid? user)
    {
        if (ent.Comp.Container == null || !_container.CanInsert(toInsert, ent.Comp.Container))
            return false;

        // Mobs are allowed to jump inside even without any hands
        if (user != null && !HasComp<HandsComponent>(user) && toInsert != user)
        {
            _popup.PopupEntity(Loc.GetString("disposal-unit-no-hands"), user.Value, user.Value, PopupType.SmallCaution);
            return false;
        }

        // Determine if the user is inserting themselves and determine the entry delay
        bool insertingSelf = (user == toInsert);
        var delay = insertingSelf ? ent.Comp.EntryDelay : ent.Comp.DraggedEntryDelay;

        // Warn other players if someone is dragging them into a disposal unit
        if (user != null && !insertingSelf)
        {
            _popup.PopupEntity(Loc.GetString("disposal-unit-being-inserted",
                ("user", Identity.Entity(user.Value, EntityManager))),
                toInsert,
                toInsert,
                PopupType.Large);
        }

        // Check if we can insert the entity instantly
        if (delay <= 0 || user == null || !HasComp<BodyComponent>(toInsert))
        {
            Insert(ent, toInsert, user);
            return true;
        }

        // If not, start a do-after
        var doAfterArgs = new DoAfterArgs(EntityManager, user.Value, delay, new DisposalDoAfterEvent(), ent, target: toInsert, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }
}
