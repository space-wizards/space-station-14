using System.Linq;
using Content.Server.Construction.Completions;
using Content.Shared.Buckle.Components;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Server.Buckle.Systems;

public sealed partial class BuckleSystem
{
    private void InitializeStrap()
    {
        SubscribeLocalEvent<StrapComponent, ComponentShutdown>(OnStrapShutdown);
        SubscribeLocalEvent<StrapComponent, ComponentRemove>((_, c, _) => StrapRemoveAll(c));
        SubscribeLocalEvent<StrapComponent, ComponentGetState>(OnStrapGetState);
        SubscribeLocalEvent<StrapComponent, EntInsertedIntoContainerMessage>(ContainerModifiedStrap);
        SubscribeLocalEvent<StrapComponent, EntRemovedFromContainerMessage>(ContainerModifiedStrap);
        SubscribeLocalEvent<StrapComponent, GetVerbsEvent<InteractionVerb>>(AddStrapVerbs);
        SubscribeLocalEvent<StrapComponent, ContainerGettingInsertedAttemptEvent>(OnStrapInsertAttempt);
        SubscribeLocalEvent<StrapComponent, InteractHandEvent>(OnStrapInteractHand);
        SubscribeLocalEvent<StrapComponent, DestructionEventArgs>((_,c,_) => StrapRemoveAll(c));
        SubscribeLocalEvent<StrapComponent, BreakageEventArgs>((_, c, _) => StrapRemoveAll(c));
        SubscribeLocalEvent<StrapComponent, ConstructionBeforeDeleteEvent>((_, c, _) => StrapRemoveAll(c));
        SubscribeLocalEvent<StrapComponent, DragDropEvent>(OnStrapDragDrop);
    }

    private void OnStrapGetState(EntityUid uid, StrapComponent component, ref ComponentGetState args)
    {
        args.State = new StrapComponentState(component.Position, component.BuckleOffset, component.BuckledEntities, component.MaxBuckleDistance);
    }

    private void ContainerModifiedStrap(EntityUid uid, StrapComponent strap, ContainerModifiedMessage message)
    {
        if (GameTiming.ApplyingState)
            return;

        foreach (var buckledEntity in strap.BuckledEntities)
        {
            if (!TryComp(buckledEntity, out BuckleComponent? buckled))
            {
                continue;
            }

            ContainerModifiedReAttach(buckledEntity, strap.Owner, buckled, strap);
        }
    }

    private void ContainerModifiedReAttach(EntityUid buckleId, EntityUid strapId, BuckleComponent? buckle = null, StrapComponent? strap = null)
    {
        if (!Resolve(buckleId, ref buckle, false) ||
            !Resolve(strapId, ref strap, false))
        {
            return;
        }

        var contained = _containers.TryGetContainingContainer(buckleId, out var ownContainer);
        var strapContained = _containers.TryGetContainingContainer(strapId, out var strapContainer);

        if (contained != strapContained || ownContainer != strapContainer)
        {
            TryUnbuckle(buckleId, buckle.Owner, true, buckle);
            return;
        }

        if (!contained)
        {
            ReAttach(buckleId, strap, buckle);
        }
    }

    private void OnStrapShutdown(EntityUid uid, StrapComponent component, ComponentShutdown args)
    {
        if (LifeStage(uid) > EntityLifeStage.MapInitialized)
            return;

        StrapRemoveAll(component);
    }

    private void OnStrapInsertAttempt(EntityUid uid, StrapComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        // If someone is attempting to put this item inside of a backpack, ensure that it has no entities strapped to it.
        if (HasComp<SharedStorageComponent>(args.Container.Owner) && component.BuckledEntities.Count != 0)
            args.Cancel();
    }

    private void OnStrapInteractHand(EntityUid uid, StrapComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        ToggleBuckle(args.User, args.User, uid);
    }

    private void AddStrapVerbs(EntityUid uid, StrapComponent strap, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract || !strap.Enabled)
            return;

        // Note that for whatever bloody reason, buckle component has its own interaction range. Additionally, this
        // range can be set per-component, so we have to check a modified InRangeUnobstructed for every verb.

        // Add unstrap verbs for every strapped entity.
        foreach (var entity in strap.BuckledEntities)
        {
            var buckledComp = Comp<BuckleComponent>(entity);

            if (!_interactions.InRangeUnobstructed(args.User, args.Target, range: buckledComp.Range))
                continue;

            InteractionVerb verb = new()
            {
                Act = () => TryUnbuckle(entity, args.User, buckle: buckledComp),
                Category = VerbCategory.Unbuckle
            };

            if (entity == args.User)
                verb.Text = Loc.GetString("verb-self-target-pronoun");
            else
                verb.Text = Comp<MetaDataComponent>(entity).EntityName;

            // In the event that you have more than once entity with the same name strapped to the same object,
            // these two verbs will be identical according to Verb.CompareTo, and only one with actually be added to
            // the verb list. However this should rarely ever be a problem. If it ever is, it could be fixed by
            // appending an integer to verb.Text to distinguish the verbs.

            args.Verbs.Add(verb);
        }

        // Add a verb to buckle the user.
        if (TryComp(args.User, out BuckleComponent? buckle) &&
            buckle.BuckledTo != strap &&
            args.User != strap.Owner &&
            StrapHasSpace(uid, buckle, strap) &&
            _interactions.InRangeUnobstructed(args.User, args.Target, range: buckle.Range))
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
        if (args.Using is {Valid: true} @using &&
            TryComp(@using, out BuckleComponent? usingBuckle) &&
            StrapHasSpace(uid, usingBuckle, strap) &&
            _interactions.InRangeUnobstructed(@using, args.Target, range: usingBuckle.Range))
        {
            // Check that the entity is unobstructed from the target (ignoring the user).
            bool Ignored(EntityUid entity) => entity == args.User || entity == args.Target || entity == @using;
            if (!_interactions.InRangeUnobstructed(@using, args.Target, usingBuckle.Range, predicate: Ignored))
                return;

            InteractionVerb verb = new()
            {
                Act = () => TryBuckle(@using, args.User, args.Target, usingBuckle),
                Category = VerbCategory.Buckle,
                Text = Comp<MetaDataComponent>(@using).EntityName,
                // just a held object, the user is probably just trying to sit down.
                // If the used entity is a person being pulled, prioritize this verb. Conversely, if it is
                Priority = HasComp<ActorComponent>(@using) ? 1 : -1
            };

            args.Verbs.Add(verb);
        }
    }

    private void StrapRemoveAll(StrapComponent strap)
    {
        foreach (var entity in strap.BuckledEntities.ToArray())
        {
            TryUnbuckle(entity, entity, true);
        }

        strap.BuckledEntities.Clear();
        strap.OccupiedSize = 0;
        Dirty(strap);
    }

    private void OnStrapDragDrop(EntityUid uid, StrapComponent component, DragDropEvent args)
    {
        if (!StrapCanDragDropOn(uid, args.User, args.Target, args.Dragged, component))
            return;

        args.Handled = TryBuckle(args.Dragged, args.User, uid);
    }

    private bool StrapHasSpace(EntityUid strapId, BuckleComponent buckle, StrapComponent? strap = null)
    {
        if (!Resolve(strapId, ref strap, false))
            return false;

        return strap.OccupiedSize + buckle.Size <= strap.Size;
    }

    private bool StrapTryAdd(EntityUid strapId, BuckleComponent buckle, bool force = false, StrapComponent? strap = null)
    {
        if (!Resolve(strapId, ref strap, false) ||
            !strap.Enabled)
        {
            return false;
        }

        if (!force && !StrapHasSpace(strapId, buckle, strap))
            return false;

        if (!strap.BuckledEntities.Add(buckle.Owner))
            return false;

        strap.OccupiedSize += buckle.Size;

        _appearance.SetData(buckle.Owner, StrapVisuals.RotationAngle, strap.Rotation);

        _appearance.SetData(strap.Owner, StrapVisuals.State, true);

        Dirty(strap);
        return true;
    }

    public void StrapSetEnabled(EntityUid strapId, bool enabled, StrapComponent? strap = null)
    {
        if (!Resolve(strapId, ref strap, false) ||
            strap.Enabled == enabled)
        {
            return;
        }

        strap.Enabled = enabled;

        if (!enabled)
            StrapRemoveAll(strap);
    }
}
