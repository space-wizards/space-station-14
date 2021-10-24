using System;
using System.Collections.Generic;
using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Construction
{
    public partial class ConstructionSystem
    {
        private readonly HashSet<EntityUid> _constructionUpdateQueue = new();

        private void InitializeSteps()
        {
            // DoAfter handling.
            // The ConstructionDoAfter events are meant to be raised either directed or broadcast.
            // If they're raised broadcast, we will re-raise them as directed on the target.
            // This allows us to easily use the DoAfter system for our purposes.
            SubscribeLocalEvent<ConstructionDoAfterComplete>(OnDoAfterComplete);
            SubscribeLocalEvent<ConstructionDoAfterCancelled>(OnDoAfterCancelled);
            SubscribeLocalEvent<ConstructionComponent, ConstructionDoAfterComplete>(EnqueueEvent);
            SubscribeLocalEvent<ConstructionComponent, ConstructionDoAfterCancelled>(EnqueueEvent);

            // Event handling. Add your subscriptions here! Just make sure they're handled by EnqueueEvent.
            SubscribeLocalEvent<ConstructionComponent, InteractUsingEvent>(EnqueueEvent);
            SubscribeLocalEvent<ConstructionComponent, InteractHandEvent>(EnqueueEvent);
        }

        /// <summary>
        ///     Takes in an entity with <see cref="ConstructionComponent"/> and an object event, and handles any
        ///     possible construction interactions, depending on the construction's state.
        /// </summary>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleEvent(EntityUid uid, object ev, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // If the state machine is in an invalid state (not on a valid node) we can't do anything, ever.
            if (GetCurrentNode(uid, construction) is not {} node)
            {
                return HandleResult.False;
            }

            // If we're currently in an edge, we'll let the edge handle the interaction.
            if (GetCurrentEdge(uid, construction) is {} edge)
            {
                return HandleEdge(uid, ev, edge, construction);
            }

            // If we're not on an edge, let the node handle the interaction.
            return HandleNode(uid, ev, node, construction);
        }

        /// <summary>
        ///     Takes in an entity, a <see cref="ConstructionGraphNode"/> and an object event, and handles any
        ///     possible construction interactions. This will check the interaction against all possible edges,
        ///     and if any of the edges accepts the interaction, we will enter it.
        /// </summary>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleNode(EntityUid uid, object ev, ConstructionGraphNode node, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // Let's make extra sure this is zero...
            construction.StepIndex = 0;

            for (var i = 0; i < node.Edges.Count; i++)
            {
                var edge = node.Edges[i];
                if (HandleEdge(uid, ev, edge, construction) is var result and (HandleResult.True or HandleResult.DoAfter))
                {
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
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleEdge(EntityUid uid, object ev, ConstructionGraphEdge edge, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            var step = GetStepFromEdge(edge, construction.StepIndex);

            if (step == null)
            {
                _sawmill.Warning($"Called {nameof(HandleEdge)} on entity {uid} but the current state is not valid for that!");
                return HandleResult.False;
            }

            if (!CheckConditions(uid, edge.Conditions))
                return HandleResult.False;

            if (HandleStep(uid, ev, step, out var user, construction)
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
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleStep(EntityUid uid, object ev, ConstructionGraphStep step, out EntityUid? user, ConstructionComponent? construction = null)
        {
            user = null;
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            if (HandleInteraction(uid, ev, step, out user, construction)
                is var handle and (HandleResult.False or HandleResult.DoAfter))
                return handle;

            PerformActions(uid, user, step.Completed);

            UpdatePathfinding(uid, construction);

            return HandleResult.True;
        }

        /// <summary>
        ///     Takes in an entity, a <see cref="ConstructionGraphStep"/> and an object event, and handles any possible
        ///     construction interaction. Unlike <see cref="HandleStep"/>, this only handles the interaction itself
        ///     and doesn't perform any step completion actions. Also sets the out parameter to the user's EntityUid.
        /// </summary>
        /// <returns>The result of this interaction with the entity.</returns>
        private HandleResult HandleInteraction(EntityUid uid, object ev, ConstructionGraphStep step, out EntityUid? user, ConstructionComponent? construction = null)
        {
            user = null;
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            // Whether this event is being re-handled after a DoAfter or not. Check DoAfterState for more info.
            var doAfterState = DoAfterState.None;

            // Custom data from a prior HandleInteraction where a DoAfter was called...
            object? doAfterData = null;

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

            // Can't perform any interactions while we're waiting for a DoAfter...
            if (construction.WaitingDoAfter)
                return HandleResult.False;

            switch (step)
            {

                // --- CONSTRUCTION STEP EVENT HANDLING START ---
                // So you want to create your own custom step for construction?
                // You're looking at the right place, then! You should create
                // a new case for your step here, and handle it as you see fit.
                // Make extra sure you handle DoAfter (if applicable) properly!
                // Note: Please use braces for your new case, it's convenient.

                case EntityInsertConstructionGraphStep insertStep:
                {
                    // EntityInsert steps only work with InteractUsing!
                    if (ev is not InteractUsingEvent interactUsing)
                        break;

                    // TODO: Sanity checks.

                    // If this step's DoAfter was cancelled, we just fail the interaction.
                    if (doAfterState == DoAfterState.Cancelled)
                        return HandleResult.False;

                    var insert = interactUsing.Used;

                    // Since many things inherit this step, we delegate the "is this entity valid?" logic to them.
                    // While this is very OOP and I find it icky, I must admit that it simplifies the code here a lot.
                    if(!insertStep.EntityValid(insert))
                        return HandleResult.False;

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
                        if (_stackSystem.Split(insert.Uid, materialInsertStep.Amount, interactUsing.User.Transform.Coordinates) is not { } stack)
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
                        _containerSystem.EnsureContainer<Container>(uid, store)
                            .Insert(insert);
                    }
                    else
                    {
                        // If we don't store the item in a container on the entity, we just delete it right away.
                        insert.Delete();
                    }

                    // Step has been handled correctly, so we signal this.
                    return HandleResult.True;
                }

                case ToolConstructionGraphStep toolInsertStep:
                {
                    if (ev is not InteractUsingEvent interactUsing)
                        break;

                    // TODO: Sanity checks.

                    user = interactUsing.User.Uid;

                    // If we're handling an event after its DoAfter finished...
                    if (doAfterState != DoAfterState.None)
                        return doAfterState == DoAfterState.Completed ? HandleResult.True : HandleResult.False;

                    if (!_toolSystem.UseTool(interactUsing.Used.Uid, interactUsing.User.Uid,
                        uid, toolInsertStep.Fuel, toolInsertStep.DoAfter, toolInsertStep.Tool,
                        new ConstructionDoAfterComplete(uid, ev), new ConstructionDoAfterCancelled(uid, ev)))
                        return HandleResult.False;

                    // In the case we're not waiting for a doAfter, then this step is complete!
                    if (toolInsertStep.DoAfter <= 0)
                        return HandleResult.True;

                    construction.WaitingDoAfter = true;
                    return HandleResult.DoAfter;
                }

                // --- CONSTRUCTION STEP EVENT HANDLING FINISH ---

                default:
                    throw new ArgumentOutOfRangeException(nameof(step),
                        "You need to code your ConstructionGraphStep behavior by adding a case to the switch.");
            }

            return HandleResult.False;
        }

        public bool CheckConditions(EntityUid uid, IEnumerable<IGraphCondition> conditions)
        {
            foreach (var condition in conditions)
            {
                if (!condition.Condition(uid, EntityManager))
                    return false;
            }

            return true;
        }

        public void PerformActions(EntityUid uid, EntityUid? userUid, IEnumerable<IGraphAction> actions)
        {
            foreach (var action in actions)
            {
                // If an action deletes the entity, we stop performing actions.
                if (!EntityManager.EntityExists(uid))
                    break;

                // TODO: Change this signature, make it not use IEntity...
                action.PerformAction(uid, userUid, EntityManager);
            }
        }

        public void ResetEdge(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return;

            construction.TargetEdgeIndex = null;
            construction.EdgeIndex = null;
            construction.StepIndex = 0;

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
                if (!EntityManager.EntityExists(uid) || !EntityManager.TryGetComponent(uid, out ConstructionComponent? construction))
                    continue;

                // Handle all queued interactions!
                while (construction.InteractionQueue.TryDequeue(out var interaction))
                {
                    HandleEvent(uid, interaction, construction);
                }
            }

            _constructionUpdateQueue.Clear();
        }

        #region Event Handlers

        private void EnqueueEvent(EntityUid uid, ConstructionComponent construction, object args)
        {
            // Handled events get treated specially.
            if (args is HandledEntityEventArgs handled)
            {
                // If they're already handled, we do nothing.
                if (handled.Handled)
                    return;

                // Otherwise, let's handle them! ...Despite the fact that we could fail the interaction later but.
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
            if (!EntityManager.EntityExists(ev.TargetUid))
                return;

            // Re-raise this event, but directed on the target UID.
            RaiseLocalEvent(ev.TargetUid, ev, false);
        }

        private void OnDoAfterCancelled(ConstructionDoAfterCancelled ev)
        {
            // Make extra sure the target entity exists...
            if (!EntityManager.EntityExists(ev.TargetUid))
                return;

            // Re-raise this event, but directed on the target UID.
            RaiseLocalEvent(ev.TargetUid, ev, false);
        }

        #endregion

        /// <summary>
        ///     This event signals that a construction interaction's DoAfter has completed successfully.
        ///     This wraps the original event and also keeps some custom data that event handlers might need.
        /// </summary>
        private class ConstructionDoAfterComplete : EntityEventArgs
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
        private class ConstructionDoAfterCancelled : EntityEventArgs
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
            ///     The interaction wasn't handled.
            /// </summary>
            False,

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
    }
}
