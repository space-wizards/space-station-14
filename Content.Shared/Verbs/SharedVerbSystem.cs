using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Robust.Shared.Containers;

namespace Content.Shared.Verbs
{
    public abstract class SharedVerbSystem : EntitySystem
    {
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeAllEvent<ExecuteVerbEvent>(HandleExecuteVerb);
        }

        private void HandleExecuteVerb(ExecuteVerbEvent args, EntitySessionEventArgs eventArgs)
        {
            var user = eventArgs.SenderSession.AttachedEntity;
            if (user == null)
                return;

            if (!TryGetEntity(args.Target, out var target))
                return;

            // It is possible that client-side prediction can cause this event to be raised after the target entity has
            // been deleted. So we need to check that the entity still exists.
            if (Deleted(user))
                return;

            // Get the list of verbs. This effectively also checks that the requested verb is in fact a valid verb that
            // the user can perform.
            var verbs = GetLocalVerbs(target.Value, user.Value, args.RequestedVerb.GetType());

            // Note that GetLocalVerbs might waste time checking & preparing unrelated verbs even though we know
            // precisely which one we want to run. However, MOST entities will only have 1 or 2 verbs of a given type.
            // The one exception here is the "other" verb type, which has 3-4 verbs + all the debug verbs.

            // Find the requested verb.
            if (verbs.TryGetValue(args.RequestedVerb, out var verb))
                ExecuteVerb(verb, user.Value, target.Value);
        }

        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s) defined in local systems. This
        ///     does not request verbs from the server.
        /// </summary>
        public SortedSet<Verb> GetLocalVerbs(EntityUid target, EntityUid user, Type type, bool force = false)
        {
            return GetLocalVerbs(target, user, new List<Type>() { type }, force);
        }

        /// <inheritdoc cref="GetLocalVerbs(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.EntityUid,System.Type,bool)"/>
        public SortedSet<Verb> GetLocalVerbs(EntityUid target, EntityUid user, List<Type> types, bool force = false)
        {
            return GetLocalVerbs(target, user, types, out _, force);
        }

        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s) defined in local systems. This
        ///     does not request verbs from the server.
        /// </summary>
        public SortedSet<Verb> GetLocalVerbs(EntityUid target, EntityUid user, List<Type> types,
            out List<VerbCategory> extraCategories, bool force = false)
        {
            SortedSet<Verb> verbs = new();
            extraCategories = new();

            // accessibility checks
            bool canAccess = false;
            if (force || target == user)
                canAccess = true;
            else if (_interactionSystem.InRangeUnobstructed(user, target))
            {
                // Note that being in a container does not count as an obstruction for InRangeUnobstructed
                // Therefore, we need extra checks to ensure the item is actually accessible:
                if (ContainerSystem.IsInSameOrParentContainer(user, target))
                    canAccess = true;
                else
                    // the item might be in a backpack that the user has open
                    canAccess = _interactionSystem.CanAccessViaStorage(user, target);
            }

            // A large number of verbs need to check action blockers. Instead of repeatedly having each system individually
            // call ActionBlocker checks, just cache it for the verb request.
            var canInteract = force || _actionBlockerSystem.CanInteract(user, target);

            EntityUid? @using = null;
            if (TryComp(user, out HandsComponent? hands) && (force || _actionBlockerSystem.CanUseHeldEntity(user)))
            {
                // if we don't actually have any hands, pass in a null value for the events.
                if (hands.Count == 0)
                {
                    hands = null;
                }
                else
                {
                    @using = hands.ActiveHandEntity;

                    // Check whether the "Held" entity is a virtual pull entity. If yes, set that as the entity being "Used".
                    // This allows you to do things like buckle a dragged person onto a surgery table, without click-dragging
                    // their sprite.

                    if (TryComp(@using, out VirtualItemComponent? pull))
                    {
                        @using = pull.BlockingEntity;
                    }
                }
            }

            // TODO: fix this garbage and use proper generics or reflection or something else, not this.
            if (types.Contains(typeof(InteractionVerb)))
            {
                var verbEvent = new GetVerbsEvent<InteractionVerb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(target, verbEvent, true);
                verbs.UnionWith(verbEvent.Verbs);
            }

            if (types.Contains(typeof(UtilityVerb))
                && @using != null
                && @using != target)
            {
                var verbEvent = new GetVerbsEvent<UtilityVerb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(@using.Value, verbEvent, true); // directed at used, not at target
                verbs.UnionWith(verbEvent.Verbs);
            }

            if (types.Contains(typeof(InnateVerb)))
            {
                var verbEvent = new GetVerbsEvent<InnateVerb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(user, verbEvent, true);
                verbs.UnionWith(verbEvent.Verbs);
            }

            if (types.Contains(typeof(AlternativeVerb)))
            {
                var verbEvent = new GetVerbsEvent<AlternativeVerb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(target, verbEvent, true);
                verbs.UnionWith(verbEvent.Verbs);
            }

            if (types.Contains(typeof(ActivationVerb)))
            {
                var verbEvent = new GetVerbsEvent<ActivationVerb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(target, verbEvent, true);
                verbs.UnionWith(verbEvent.Verbs);
            }

            if (types.Contains(typeof(ExamineVerb)))
            {
                var verbEvent = new GetVerbsEvent<ExamineVerb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(target, verbEvent, true);
                verbs.UnionWith(verbEvent.Verbs);
            }

            // generic verbs
            if (types.Contains(typeof(Verb)))
            {
                var verbEvent = new GetVerbsEvent<Verb>(user, target, @using, hands, canInteract, canAccess, extraCategories);
                RaiseLocalEvent(target, verbEvent, true);
                verbs.UnionWith(verbEvent.Verbs);
            }

            if (types.Contains(typeof(EquipmentVerb)))
            {
                var access = canAccess || _interactionSystem.CanAccessEquipment(user, target);
                var verbEvent = new GetVerbsEvent<EquipmentVerb>(user, target, @using, hands, canInteract, access, extraCategories);
                RaiseLocalEvent(target, verbEvent);
                verbs.UnionWith(verbEvent.Verbs);
            }

            return verbs;
        }

        /// <summary>
        ///     Execute the provided verb.
        /// </summary>
        /// <remarks>
        ///     This will try to call the action delegates and raise the local events for the given verb.
        /// </remarks>
        public virtual void ExecuteVerb(Verb verb, EntityUid user, EntityUid target, bool forced = false)
        {
            // invoke any relevant actions
            verb.Act?.Invoke();

            // Maybe raise a local event
            if (verb.ExecutionEventArgs != null)
            {
                if (verb.EventTarget.IsValid())
                    RaiseLocalEvent(verb.EventTarget, verb.ExecutionEventArgs);
                else
                    RaiseLocalEvent(verb.ExecutionEventArgs);
            }

            if (Deleted(user) || Deleted(target))
                return;

            // Perform any contact interactions
            if (verb.DoContactInteraction ?? (verb.DefaultDoContactInteraction && _interactionSystem.InRangeUnobstructed(user, target)))
                _interactionSystem.DoContactInteraction(user, target);
        }
    }
}
