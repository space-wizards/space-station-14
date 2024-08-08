using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Construction.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Construction.Steps;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Prying.Systems;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Temperature;
using Content.Shared.Tools.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
#if EXCEPTION_TOLERANCE
// ReSharper disable once RedundantUsingDirective
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

        private readonly Queue<EntityUid> _constructionUpdateQueue = new();
        private readonly HashSet<EntityUid> _queuedUpdates = new();

        private void InitializeInteractions()
        {
            SubscribeLocalEvent<ConstructionComponent, ConstructionInteractDoAfterEvent>(EnqueueEvent);

            // Event handling. Add your subscriptions here! Just make sure they're all handled by EnqueueEvent.
            SubscribeLocalEvent<ConstructionComponent, InteractUsingEvent>(EnqueueEvent,
                new []{typeof(AnchorableSystem), typeof(PryingSystem), typeof(WeldableSystem)},
                new []{typeof(EncryptionKeySystem)});
            SubscribeLocalEvent<ConstructionComponent, OnTemperatureChangeEvent>(EnqueueEvent);
            SubscribeLocalEvent<ConstructionComponent, PartAssemblyPartInsertedEvent>(EnqueueEvent);
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
                Log.Warning($"Called {nameof(HandleEdge)} on entity {ToPrettyString(uid)} but the current state is not valid for that!");
                return HandleResult.False;
            }

            // We need to ensure we currently satisfy any and all edge conditions.
            if (!CheckConditions(uid, edge.Conditions))
                return HandleResult.False;

            var handle = HandleStep(uid, ev, step, validation, out var user, construction);
            if (handle is not HandleResult.True)
                return handle;

            // Handle step should never handle the interaction during validation.
            DebugTools.Assert(!validation);

            // We increase the step index, meaning we move to the next step!
            construction.StepIndex++;

            // Check if the new step index is greater than the amount of steps in the edge...
            if (construction.StepIndex >= edge.Steps.Count)
            {
                // Edge finished!
                PerformActions(uid, user, edge.Completed);

                if (construction.Deleted)
                    return HandleResult.True;

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
            var handle = HandleInteraction(uid, ev, step, validation, out user, construction);
            if (handle is not HandleResult.True)
                return handle;

            DebugTools.Assert(!validation);

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
            var doAfterState = DoAfterState.None;

            // The DoAfter events can only perform special logic when we're not validating events.
            if (ev is ConstructionInteractDoAfterEvent interactDoAfter)
            {
                if (interactDoAfter.Cancelled)
                    return HandleResult.False;

                ev = new InteractUsingEvent(
                    interactDoAfter.User,
                    interactDoAfter.Used!.Value,
                    uid,
                    GetCoordinates(interactDoAfter.ClickLocation));

                doAfterState = DoAfterState.Completed;
            }

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

                    var insert = interactUsing.Used;

                    // Since many things inherit this step, we delegate the "is this entity valid?" logic to them.
                    // While this is very OOP and I find it icky, I must admit that it simplifies the code here a lot.
                    if(!insertStep.EntityValid(insert, EntityManager, _factory))
                        return HandleResult.False;

                    // If we're only testing whether this step would be handled by the given event, then we're done.
                    if (validation)
                        return HandleResult.Validated;

                    // If we still haven't completed this step's DoAfter...
                    if (doAfterState == DoAfterState.None && insertStep.DoAfter > 0)
                    {
                        var doAfterEv = new ConstructionInteractDoAfterEvent(EntityManager, interactUsing);

                        var doAfterEventArgs = new DoAfterArgs(EntityManager, interactUsing.User, step.DoAfter, doAfterEv, uid, uid, interactUsing.Used)
                        {
                            BreakOnDamage = false,
                            BreakOnMove = true,
                            NeedHand = true
                        };

                        var started  = _doAfterSystem.TryStartDoAfter(doAfterEventArgs);

                        if (!started)
                            return HandleResult.False;

#if DEBUG
                        // Verify that the resulting DoAfter event will be handled by the current construction state.
                        // if it can't what is even the point of raising this DoAfter?
                        doAfterEv.DoAfter = new(default, doAfterEventArgs, default);
                        var result = HandleInteraction(uid, doAfterEv, step, validation: true, out _, construction);
                        DebugTools.Assert(result == HandleResult.Validated);
#endif
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
                        _container.Insert(insert, _container.EnsureContainer<Container>(uid, store));
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
                    if (validation)
                    {
                        // Then we only really need to check whether the tool entity has that quality or not.
                        return _toolSystem.HasQuality(interactUsing.Used, toolInsertStep.Tool)
                            ? HandleResult.Validated
                            : HandleResult.False;
                    }

                    // If we're handling an event after its DoAfter finished...
                    if (doAfterState == DoAfterState.Completed)
                        return  HandleResult.True;

                    var result  = _toolSystem.UseTool(
                        interactUsing.Used,
                        interactUsing.User,
                        uid,
                        TimeSpan.FromSeconds(toolInsertStep.DoAfter),
                        new [] { toolInsertStep.Tool },
                        new ConstructionInteractDoAfterEvent(EntityManager, interactUsing),
                        out var doAfter,
                        toolInsertStep.Fuel);

                    return result && doAfter != null ? HandleResult.DoAfter : HandleResult.False;
                }

                case TemperatureConstructionGraphStep temperatureChangeStep:
                {
                    if (ev is not OnTemperatureChangeEvent)
                        break;

                    // Some things, like microwaves, might need to block the temperature construction step from kicking in, or override it entirely.
                    var tempEvent = new OnConstructionTemperatureEvent();
                    RaiseLocalEvent(uid, tempEvent, true);

                    if (tempEvent.Result is not null)
                        return tempEvent.Result.Value;

                    // prefer using InternalTemperature since that's more accurate for cooking.
                    float temp;
                    if (TryComp<InternalTemperatureComponent>(uid, out var internalTemp))
                    {
                        temp = internalTemp.Temperature;
                    }
                    else if (TryComp<TemperatureComponent>(uid, out var tempComp))
                    {
                        temp = tempComp.CurrentTemperature;
                    }
                    else
                    {
                        return HandleResult.False;
                    }

                    if ((!temperatureChangeStep.MinTemperature.HasValue || temp >= temperatureChangeStep.MinTemperature.Value) &&
                        (!temperatureChangeStep.MaxTemperature.HasValue || temp <= temperatureChangeStep.MaxTemperature.Value))
                    {
                        return HandleResult.True;
                    }

                    return HandleResult.False;
                }

                case PartAssemblyConstructionGraphStep partAssemblyStep:
                {
                    if (ev is not PartAssemblyPartInsertedEvent)
                        break;

                    if (partAssemblyStep.Condition(uid, EntityManager))
                        return HandleResult.True;
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
            while (_constructionUpdateQueue.TryDequeue(out var uid))
            {
                _queuedUpdates.Remove(uid);

                // Ensure the entity exists and has a Construction component.
                if (!TryComp(uid, out ConstructionComponent? construction))
                    continue;

#if EXCEPTION_TOLERANCE
                try
                {
#endif

                // Handle all queued interactions!
                while (construction.InteractionQueue.TryDequeue(out var interaction))
                {
                    if (construction.Deleted)
                    {
                        Log.Error($"Construction component was deleted while still processing interactions." +
                                  $"Entity {ToPrettyString(uid)}, graph: {construction.Graph}, " +
                                  $"Next: {interaction.GetType().Name}, " +
                                  $"Remaining Queue: {string.Join(", ", construction.InteractionQueue.Select(x => x.GetType().Name))}");
                        break;
                    }

                    // We set validation to false because we actually want to perform the interaction here.
                    HandleEvent(uid, interaction, false, construction);
                }
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Log.Error($"Caught exception while processing construction queue. Entity {ToPrettyString(uid)}, graph: {construction.Graph}");
                    _runtimeLog.LogException(e, $"{nameof(ConstructionSystem)}.{nameof(UpdateInteractions)}");
                    Del(uid);
                }
#endif
            }

            DebugTools.Assert(_queuedUpdates.Count == 0);
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
            // For handled events, we will check if the event leads to a valid construction interaction.
            // If it does, we mark the event as handled and then enqueue it as normal.
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
            if (_queuedUpdates.Add(uid))
                _constructionUpdateQueue.Enqueue(uid);
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
            ///     If Completed, this is the second (and last) time we're seeing this event, and
            ///     the doAfter that was called the first time successfully completed. Handle completion logic now.
            /// </summary>
            Completed
        }
    }

    /// <summary>
    ///     Specifies the result after attempting to handle a specific step with an event.
    /// </summary>
    public enum HandleResult : byte
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

    public sealed class OnConstructionTemperatureEvent : HandledEntityEventArgs
    {
        public HandleResult? Result;
    }
}
