using Content.Server.Administration.Logs;
using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Construction;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Construction.Steps;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Content.Server.Construction
{
    public sealed partial class ConstructionSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        private readonly HashSet<EntityUid> _constructionUpdateQueue = new();

        private void InitializeInteractions()
        {
            #region DoAfter Subscriptions

            // DoAfter handling.
            // The ConstructionDoAfter events are meant to be raised either directed or broadcast.
            // If they're raised broadcast, we will re-raise them as directed on the target.
            // This allows us to easily use the DoAfter system for our purposes.
            SubscribeLocalEvent<ConstructionDoAfterComplete>(OnDoAfterComplete);
            SubscribeLocalEvent<ConstructionDoAfterCancelled>(OnDoAfterCancelled);
            SubscribeLocalEvent<ConstructionComponent, ConstructionDoAfterComplete>(EnqueueEvent);
            SubscribeLocalEvent<ConstructionComponent, ConstructionDoAfterCancelled>(EnqueueEvent);

            #endregion

            // Event handling. Add your subscriptions here! Just make sure they're all handled by EnqueueEvent.
            SubscribeLocalEvent<ConstructionComponent, InteractUsingEvent>(EnqueueEvent, new []{typeof(AnchorableSystem)});
            SubscribeLocalEvent<ConstructionComponent, OnTemperatureChangeEvent>(EnqueueEvent);
        }

        /// <summary>
        ///     Takes in an entity with <see cref="ConstructionComponent"/> and an object event, and handles any
        ///     possible construction interactions, depending on the construction's state.
        /// </summary>
        /// <remarks>When <see cref="validation"/> is true, this method will simply return whether the interaction
        ///          would be handled by the entity or not. It essentially becomes a pure method that modifies nothing.</remarks>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleEvent(EntityUid uid, object ev, bool validation, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // If the state machine is in an invalid state (not on a valid node) we can't do anything, ever.
            if (GetCurrentNode(uid, construction) is not {} node)
            {
                return HandleResult.False;
            }

            // If we're currently in an edge, we'll let the edge handle or validate the interaction.
            if (GetCurrentEdge(uid, construction) is {} edge)
            {
                var result = HandleEdge(uid, ev, edge, validation, construction);

                // Reset edge index to none if this failed...
                if (!validation && result is HandleResult.False && construction.StepIndex == 0)
                    construction.EdgeIndex = null;

                return result;
            }

            // If we're not on an edge, let the node handle or validate the interaction.
            return HandleNode(uid, ev, node, validation, construction);
        }

        /// <summary>
        ///     Takes in an entity, a <see cref="ConstructionGraphNode"/> and an object event, and handles any
        ///     possible construction interactions. This will check the interaction against all possible edges,
        ///     and if any of the edges accepts the interaction, we will enter it.
        /// </summary>
        /// <remarks>When <see cref="validation"/> is true, this method will simply return whether the interaction
        ///          would be handled by the entity or not. It essentially becomes a pure method that modifies nothing.</remarks>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleNode(EntityUid uid, object ev, ConstructionGraphNode node, bool validation, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // Let's make extra sure this is zero...
            construction.StepIndex = 0;

            // When we handle a node, we're essentially testing the current event interaction against all of this node's
            // edges' first steps. If any of them accepts the interaction, we stop iterating and enter that edge.
            for (var i = 0; i < node.Edges.Count; i++)
            {
                var edge = node.Edges[i];
                if (HandleEdge(uid, ev, edge, validation, construction) is var result and not HandleResult.False)
                {
                    // Only a True result may modify the state.
                    // In the case of DoAfter, it's only allowed to modify the waiting flag and the current edge index.
                    // In the case of validated, it should NEVER modify the state at all.
                    if (result is not HandleResult.True)
                    {
                        if (result is HandleResult.DoAfter)
                        {
                            construction.EdgeIndex = i;
                        }

                        return result;
                    }

                    // If we're not on the same edge as we were before, that means handling that edge changed the node.
                    if (construction.Node != node.Name)
                        return result;

                    // If we're still in the same node, that means we entered the edge and it's still not done.
                    construction.EdgeIndex = i;
                    UpdatePathfinding(uid, construction);

                    return result;
                }
            }

            return HandleResult.False;
        }

        /// <summary>
        ///     Takes in an entity, a <see cref="ConstructionGraphEdge"/> and an object event, and handles any
        ///     possible construction interactions. This will check the interaction against one of the steps in the edge
        ///     depending on the construction's <see cref="ConstructionComponent.StepIndex"/>.
        /// </summary>
        /// <remarks>When <see cref="validation"/> is true, this method will simply return whether the interaction
        ///          would be handled by the entity or not. It essentially becomes a pure method that modifies nothing.</remarks>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleEdge(EntityUid uid, object ev, ConstructionGraphEdge edge, bool validation, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            var step = GetStepFromEdge(edge, construction.StepIndex);

            if (step == null)
            {
                _sawmill.Warning($"Called {nameof(HandleEdge)} on entity {uid} but the current state is not valid for that!");
                return HandleResult.False;
            }

            // We need to ensure we currently satisfy any and all edge conditions.
            if (!CheckConditions(uid, edge.Conditions))
                return HandleResult.False;

            // We can only perform the "step completed" logic if this returns true.
            if (HandleStep(uid, ev, step, validation, out var user, construction)
                is var handle and not HandleResult.True)
                return handle;

            // We increase the step index, meaning we move to the next step!
            construction.StepIndex++;

            // Check if the new step index is greater than the amount of steps in the edge...
            if (construction.StepIndex >= edge.Steps.Count)
            {
                // Edge finished!
                PerformActions(uid, user, edge.Completed);
                construction.TargetEdgeIndex = null;
                construction.EdgeIndex = null;
                construction.StepIndex = 0;

                // We change the node now.
                ChangeNode(uid, user, edge.Target, true, construction);
            }

            return HandleResult.True;
        }

        /// <summary>
        ///     Takes in an entity, a <see cref="ConstructionGraphStep"/> and an object event, and handles any possible
        ///     construction interaction. Unlike <see cref="HandleInteraction"/>, if this succeeds it will perform the
        ///     step's completion actions. Also sets the out parameter to the user's EntityUid.
        /// </summary>
        /// <remarks>When <see cref="validation"/> is true, this method will simply return whether the interaction
        ///          would be handled by the entity or not. It essentially becomes a pure method that modifies nothing.</remarks>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleStep(EntityUid uid, object ev, ConstructionGraphStep step, bool validation, out EntityUid? user, ConstructionComponent? construction = null)
        {
            user = null;
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // Let HandleInteraction actually handle the event for this step.
            // We can only perform the rest of our logic if it returns true.
            if (HandleInteraction(uid, ev, step, validation, out user, construction)
                is var handle and not HandleResult.True)
                return handle;

            // Actually perform the step completion actions, since the step was handled correctly.
            PerformActions(uid, user, step.Completed);

            UpdatePathfinding(uid, construction);

            return HandleResult.True;
        }

        /// <summary>
        ///     Takes in an entity, a <see cref="ConstructionGraphStep"/> and an object event, and handles any possible
        ///     construction interaction. Unlike <see cref="HandleStep"/>, this only handles the interaction itself
        ///     and doesn't perform any step completion actions. Also sets the out parameter to the user's EntityUid.
        /// </summary>
        /// <remarks>When <see cref="validation"/> is true, this method will simply return whether the interaction
        ///          would be handled by the entity or not. It essentially becomes a pure method that modifies nothing.</remarks>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleInteraction(EntityUid uid, object ev, ConstructionGraphStep step, bool validation, out EntityUid? user, ConstructionComponent? construction = null)
        {
            user = null;
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // Whether this event is being re-handled after a DoAfter or not. Check DoAfterState for more info.
            var doAfterState = validation ? DoAfterState.Validation : DoAfterState.None;

            // Custom data from a prior HandleInteraction where a DoAfter was called...
            object? doAfterData = null;

            // The DoAfter events can only perform special logic when we're not validating events.
            if (!validation)
            {
                // Some events are handled specially... Such as doAfter.
                switch (ev)
                {
                    case ConstructionDoAfterComplete complete:
                    {
                        // DoAfter completed!
                        ev = complete.WrappedEvent;
                        doAfterState = DoAfterState.Completed;
                        doAfterData = complete.CustomData;
                        construction.WaitingDoAfter = false;
                        break;
                    }

                    case ConstructionDoAfterCancelled cancelled:
                    {
                        // DoAfter failed!
                        ev = cancelled.WrappedEvent;
                        doAfterState = DoAfterState.Cancelled;
                        doAfterData = cancelled.CustomData;
                        construction.WaitingDoAfter = false;
                        break;
                    }
                }
            }

            // Can't perform any interactions while we're waiting for a DoAfter...
            // This also makes any event validation fail.
            if (construction.WaitingDoAfter)
                return HandleResult.False;

            // The cases in this switch will handle the interaction and return
            switch (step)
            {

                // --- CONSTRUCTION STEP EVENT HANDLING START ---
                #region Construction Step Event Handling
                // So you want to create your own custom step for construction?
                // You're looking at the right place, then! You should create
                // a new case for your step here, and handle it as you see fit.
                // Make extra sure you handle DoAfter (if applicable) properly!
                // Also make sure your event handler properly handles validation.
                // Note: Please use braces for your new case, it's convenient.

                case EntityInsertConstructionGraphStep insertStep:
                {
                    // EntityInsert steps only work with InteractUsing!
                    if (ev is not InteractUsingEvent interactUsing)
                        break;

                    // TODO: Sanity checks.

                    user = interactUsing.User;

                    // If this step's DoAfter was cancelled, we just fail the interaction.
                    if (doAfterState == DoAfterState.Cancelled)
                        return HandleResult.False;

                    var insert = interactUsing.Used;

                    // Since many things inherit this step, we delegate the "is this entity valid?" logic to them.
                    // While this is very OOP and I find it icky, I must admit that it simplifies the code here a lot.
                    if(!insertStep.EntityValid(insert, EntityManager, _factory))
                        return HandleResult.False;

                    // If we're only testing whether this step would be handled by the given event, then we're done.
                    if (doAfterState == DoAfterState.Validation)
                        return HandleResult.Validated;

                    // If we still haven't completed this step's DoAfter...
                    if (doAfterState == DoAfterState.None && insertStep.DoAfter > 0)
                    {
                        _doAfterSystem.DoAfter(
                            new DoAfterEventArgs(interactUsing.User, step.DoAfter, default, interactUsing.Target)
                            {
                                BreakOnDamage = false,
                                BreakOnStun = true,
                                BreakOnTargetMove = true,
                                BreakOnUserMove = true,
                                NeedHand = true,

                                // These events will be broadcast and handled by this very same system, that will
                                // raise them directed to the target. These events wrap the original event.
                                BroadcastFinishedEvent = new ConstructionDoAfterComplete(uid, ev),
                                BroadcastCancelledEvent = new ConstructionDoAfterCancelled(uid, ev)
                            });

                        // To properly signal that we're waiting for a DoAfter, we have to set the flag on the component
                        // and then also return the DoAfter HandleResult.
                        construction.WaitingDoAfter = true;
                        return HandleResult.DoAfter;
                    }

                    // Material steps, which use stacks, are handled specially. Instead of inserting the whole item,
                    // we split the stack in two and insert the split stack.
                    if (insertStep is MaterialConstructionGraphStep materialInsertStep)
                    {
                        if (_stackSystem.Split(insert, materialInsertStep.Amount, Transform(interactUsing.User).Coordinates) is not {} stack)
                            return HandleResult.False;

                        insert = stack;
                    }

                    // Container-storage handling.
                    if (!string.IsNullOrEmpty(insertStep.Store))
                    {
                        // In the case we want to store this item in a container on the entity...
                        var store = insertStep.Store;

                        // Add this container to the collection of "construction-owned" containers.
                        // Containers in that set will be transferred to new entities in the case of a prototype change.
                        construction.Containers.Add(store);

                        // The container doesn't necessarily need to exist, so we ensure it.
                        _container.EnsureContainer<Container>(uid, store).Insert(insert);
                    }
                    else
                    {
                        // If we don't store the item in a container on the entity, we just delete it right away.
                        Del(insert);
                    }

                    // Step has been handled correctly, so we signal this.
                    return HandleResult.True;
                }

                case ToolConstructionGraphStep toolInsertStep:
                {
                    if (ev is not InteractUsingEvent interactUsing)
                        break;

                    // TODO: Sanity checks.

                    user = interactUsing.User;

                    // If we're validating whether this event handles the step...
                    if (doAfterState == DoAfterState.Validation)
                    {
                        // Then we only really need to check whether the tool entity has that quality or not.
                        return _toolSystem.HasQuality(interactUsing.Used, toolInsertStep.Tool)
                            ? HandleResult.Validated : HandleResult.False;
                    }

                    // If we're handling an event after its DoAfter finished...
                    if (doAfterState != DoAfterState.None)
                        return doAfterState == DoAfterState.Completed ? HandleResult.True : HandleResult.False;

                    if (!_toolSystem.UseTool(interactUsing.Used, interactUsing.User,
                        uid, toolInsertStep.Fuel, toolInsertStep.DoAfter, toolInsertStep.Tool,
                        new ConstructionDoAfterComplete(uid, ev), new ConstructionDoAfterCancelled(uid, ev)))
                        return HandleResult.False;

                    // In the case we're not waiting for a doAfter, then this step is complete!
                    if (toolInsertStep.DoAfter <= 0)
                        return HandleResult.True;

                    construction.WaitingDoAfter = true;
                    return HandleResult.DoAfter;
                }

                case TemperatureConstructionGraphStep temperatureChangeStep:
                {
                    if (ev is not OnTemperatureChangeEvent) {
                        break;
                    }

                    if (TryComp<TemperatureComponent>(uid, out var tempComp))
                    {
                        if ((!temperatureChangeStep.MinTemperature.HasValue || tempComp.CurrentTemperature >= temperatureChangeStep.MinTemperature.Value) &&
                            (!temperatureChangeStep.MaxTemperature.HasValue || tempComp.CurrentTemperature <= temperatureChangeStep.MaxTemperature.Value))
                        {
                            return HandleResult.True;
                        }
                    }
                    return HandleResult.False;

                }

                #endregion
                // --- CONSTRUCTION STEP EVENT HANDLING FINISH ---

                default:
                    throw new ArgumentOutOfRangeException(nameof(step),
                        "You need to code your ConstructionGraphStep behavior by adding a case to the switch.");
            }

            // If the handlers were not able to handle this event, return...
            return HandleResult.False;
        }

        /// <summary>
        ///     Checks whether a number of <see cref="IGraphCondition"/>s are true for a given entity.
        /// </summary>
        /// <param name="uid">The entity to pass to the conditions.</param>
        /// <param name="conditions">The conditions to evaluate.</param>
        /// <remarks>This method is short-circuiting; if a condition evaluates to false, we stop checking the rest of conditions.</remarks>
        /// <returns>Whether all conditions evaluate to true for the given entity.</returns>
        public bool CheckConditions(EntityUid uid, IEnumerable<IGraphCondition> conditions)
        {
            foreach (var condition in conditions)
            {
                if (!condition.Condition(uid, EntityManager))
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Performs a number of <see cref="IGraphAction"/>s for a given entity, with an optional user entity.
        /// </summary>
        /// <param name="uid">The entity to perform the actions on.</param>
        /// <param name="userUid">An optional user entity to pass into the actions.</param>
        /// <param name="actions">The actions to perform.</param>
        /// <remarks>This method checks whether the given target entity exists before performing any actions.
        ///          If the entity is deleted by an action, it will short-circuit and stop performing the rest of actions.</remarks>
        public void PerformActions(EntityUid uid, EntityUid? userUid, IEnumerable<IGraphAction> actions)
        {
            foreach (var action in actions)
            {
                // If an action deletes the entity, we stop performing the rest of actions.
                if (!Exists(uid))
                    break;

                action.PerformAction(uid, userUid, EntityManager);
            }
        }

        /// <summary>
        ///     Resets the current construction edge status on an entity.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component. If null, it will be resolved on the entity.</param>
        /// <remarks>This method updates the construction pathfinding on the entity automatically.</remarks>
        public void ResetEdge(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;

            construction.TargetEdgeIndex = null;
            construction.EdgeIndex = null;
            construction.StepIndex = 0;

            // Update pathfinding to keep it in sync with the current construction status.
            UpdatePathfinding(uid, construction);
        }

        private void UpdateInteractions()
        {
            // We iterate all entities waiting for their interactions to be handled.
            // This is much more performant than making an EntityQuery for ConstructionComponent,
            // since, for example, every single wall has a ConstructionComponent....
            foreach (var uid in _constructionUpdateQueue)
            {
                // Ensure the entity exists and has a Construction component.
                if (!Exists(uid) || !TryComp(uid, out ConstructionComponent? construction))
                    continue;

#if EXCEPTION_TOLERANCE
                try
                {
#endif
                // Handle all queued interactions!
                while (construction.InteractionQueue.TryDequeue(out var interaction))
                {
                    // We set validation to false because we actually want to perform the interaction here.
                    HandleEvent(uid, interaction, false, construction);
                }
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception while processing construction queue. Entity {ToPrettyString(uid)}, graph: {construction.Graph}");
                    _runtimeLog.LogException(e, $"{nameof(ConstructionSystem)}.{nameof(UpdateInteractions)}");
                    Del(uid);
                }
#endif
            }

            _constructionUpdateQueue.Clear();
        }

        #region Event Handlers

        /// <summary>
        ///     Queues a directed event to be handled by construction on the next update tick.
        ///     Used as a handler for any events that construction can listen to. <seealso cref="InitializeInteractions"/>
        /// </summary>
        /// <param name="uid">The entity the event is directed to.</param>
        /// <param name="construction">The construction component to queue the event on.</param>
        /// <param name="args">The directed event to be queued.</param>
        /// <remarks>Events inheriting <see cref="HandledEntityEventArgs"/> are treated specially by this method.
        ///          They will only be queued if they can be validated against the current construction state,
        ///          in which case they will also be set as handled.</remarks>
        private void EnqueueEvent(EntityUid uid, ConstructionComponent construction, object args)
        {
            // Handled events get treated specially.
            if (args is HandledEntityEventArgs handled)
            {
                // If they're already handled, we do nothing.
                if (handled.Handled)
                    return;

                // Otherwise, let's check if this event could be handled by the construction's current state.
                if (HandleEvent(uid, args, true, construction) != HandleResult.Validated)
                    return; // Not validated, so we don't even enqueue this event.

                handled.Handled = true;
            }

            // Enqueue this event so it'll be handled in the next tick.
            // This prevents some issues that could occur from entity deletion, component deletion, etc in a handler.
            construction.InteractionQueue.Enqueue(args);

            // Add this entity to the queue so it'll be updated next tick.
            _constructionUpdateQueue.Add(uid);
        }

        private void OnDoAfterComplete(ConstructionDoAfterComplete ev)
        {
            // Make extra sure the target entity exists...
            if (!Exists(ev.TargetUid))
                return;

            // Re-raise this event, but directed on the target UID.
            RaiseLocalEvent(ev.TargetUid, ev, false);
        }

        private void OnDoAfterCancelled(ConstructionDoAfterCancelled ev)
        {
            // Make extra sure the target entity exists...
            if (!Exists(ev.TargetUid))
                return;

            // Re-raise this event, but directed on the target UID.
            RaiseLocalEvent(ev.TargetUid, ev, false);
        }

        #endregion

        #region Event Definitions

        /// <summary>
        ///     This event signals that a construction interaction's DoAfter has completed successfully.
        ///     This wraps the original event and also keeps some custom data that event handlers might need.
        /// </summary>
        private sealed class ConstructionDoAfterComplete : EntityEventArgs
        {
            public readonly EntityUid TargetUid;
            public readonly object WrappedEvent;
            public readonly object? CustomData;

            public ConstructionDoAfterComplete(EntityUid targetUid, object wrappedEvent, object? customData = null)
            {
                TargetUid = targetUid;
                WrappedEvent = wrappedEvent;
                CustomData = customData;
            }
        }

        /// <summary>
        ///     This event signals that a construction interaction's DoAfter has failed or has been cancelled.
        ///     This wraps the original event and also keeps some custom data that event handlers might need.
        /// </summary>
        private sealed class ConstructionDoAfterCancelled : EntityEventArgs
        {
            public readonly EntityUid TargetUid;
            public readonly object WrappedEvent;
            public readonly object? CustomData;

            public ConstructionDoAfterCancelled(EntityUid targetUid, object wrappedEvent, object? customData = null)
            {
                TargetUid = targetUid;
                WrappedEvent = wrappedEvent;
                CustomData = customData;
            }
        }

        #endregion

        #region Internal Enum Definitions

        /// <summary>
        ///     Specifies the DoAfter status for a construction step event handler.
        /// </summary>
        private enum DoAfterState : byte
        {
            /// <summary>
            ///     If None, this is the first time we're seeing this event and we might want to call a DoAfter
            ///     if the step needs it.
            /// </summary>
            None,

            /// <summary>
            ///     If Validation, we want to validate whether the specified event would handle the step or not.
            ///     Will NOT modify the construction state at all.
            /// </summary>
            Validation,

            /// <summary>
            ///     If Completed, this is the second (and last) time we're seeing this event, and
            ///     the doAfter that was called the first time successfully completed. Handle completion logic now.
            /// </summary>
            Completed,

            /// <summary>
            ///     If Cancelled, this is the second (and last) time we're seeing this event, and
            ///     the doAfter that was called the first time was cancelled. Handle cleanup logic now.
            /// </summary>
            Cancelled
        }

        /// <summary>
        ///     Specifies the result after attempting to handle a specific step with an event.
        /// </summary>
        private enum HandleResult : byte
        {
            /// <summary>
            ///     The interaction wasn't handled or validated.
            /// </summary>
            False,

            /// <summary>
            ///     The interaction would be handled successfully. Nothing was modified.
            /// </summary>
            Validated,

            /// <summary>
            ///     The interaction was handled successfully.
            /// </summary>
            True,

            /// <summary>
            ///     The interaction is waiting on a DoAfter now.
            ///     This means the interaction started the DoAfter.
            /// </summary>
            DoAfter,
        }

        #endregion
    }
}
