using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Climbing.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers;

public sealed partial class DragInsertContainerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragInsertContainerComponent, DragDropTargetEvent>(OnDragDropOn, before: new []{ typeof(ClimbSystem)});
        SubscribeLocalEvent<DragInsertContainerComponent, DragInsertContainerDoAfterEvent>(OnDragFinished);
        SubscribeLocalEvent<DragInsertContainerComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DragInsertContainerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
    }

    private void OnDragDropOn(Entity<DragInsertContainerComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        var (_, comp) = ent;
        if (!_container.TryGetContainer(ent, comp.ContainerId, out var container))
            return;

        if (comp.EntryDelay <= TimeSpan.Zero ||
            !comp.DelaySelfEntry && args.User == args.Dragged)
        {
            //instant insertion
            args.Handled = Insert(args.Dragged, args.User, ent, container);
            return;
        }

        //delayed insertion
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.EntryDelay, new DragInsertContainerDoAfterEvent(), ent, args.Dragged, ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragFinished(Entity<DragInsertContainerComponent> ent, ref DragInsertContainerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        Insert(args.Args.Target.Value, args.User, ent, container);
    }

    private void OnCanDragDropOn(Entity<DragInsertContainerComponent> ent, ref CanDropTargetEvent args)
    {
        var (_, comp) = ent;
        if (!_container.TryGetContainer(ent, comp.ContainerId, out var container))
            return;

        args.Handled = true;
        args.CanDrop |= _container.CanInsert(args.Dragged, container);
    }

    private void OnGetAlternativeVerb(Entity<DragInsertContainerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var (uid, comp) = ent;
        if (!comp.UseVerbs)
            return;

        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        if (!_container.TryGetContainer(uid, comp.ContainerId, out var container))
            return;

        var user = args.User;
        if (!_actionBlocker.CanInteract(user, ent))
            return;

        // Eject verb
        if (container.ContainedEntities.Count > 0)
        {
            // make sure that we can actually take stuff out of the container
            var emptyableCount = 0;
            foreach (var contained in container.ContainedEntities)
            {
                if (!_container.CanRemove(contained, container))
                    continue;
                emptyableCount++;
            }

            if (emptyableCount > 0)
            {
                AlternativeVerb verb = new()
                {
                    Act = () =>
                    {
                        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):player} emptied container {ToPrettyString(ent)}");
                        var ents = _container.EmptyContainer(container);
                        foreach (var contained in ents)
                        {
                            _climb.ForciblySetClimbing(contained, ent);
                        }
                    },
                    Category = VerbCategory.Eject,
                    Text = Loc.GetString("container-verb-text-empty"),
                    Priority = 1 // Promote to top to make ejecting the ALT-click action
                };
                args.Verbs.Add(verb);
            }
        }

        // Self-insert verb
        if (_container.CanInsert(user, container) &&
            _actionBlocker.CanMove(user))
        {
            AlternativeVerb verb = new()
            {
                Act = () => Insert(user, user, ent, container),
                Text = Loc.GetString("container-verb-text-enter"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }

    public bool Insert(EntityUid target, EntityUid user, EntityUid containerEntity, BaseContainer container)
    {
        if (!_container.Insert(target, container))
            return false;

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} inserted {ToPrettyString(target):player} into container {ToPrettyString(containerEntity)}");
        return true;
    }

    [Serializable, NetSerializable]
    public sealed partial class DragInsertContainerDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
