using System;
using System.Collections.Generic;
using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Construction
{
    public partial class ConstructionSystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        private void InitializeSteps()
        {
            SubscribeLocalEvent<ConstructionComponent, InteractUsingEvent>(EnqueueEvent);
            SubscribeLocalEvent<ConstructionComponent, InteractHandEvent>(EnqueueEvent);
        }

        private HandleResult HandleEvent(EntityUid uid, object ev, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            if (GetCurrentNode(uid, construction) is not {} node)
            {
                return HandleResult.False;
            }

            if (GetCurrentEdge(uid, construction) is {} edge)
            {
                return HandleEdge(uid, ev, edge, construction);
            }

            return HandleNode(uid, ev, node, construction);
        }

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
                    return result;
                }
            }

            return HandleResult.False;
        }

        private HandleResult HandleEdge(EntityUid uid, object ev, ConstructionGraphEdge edge, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            var step = GetCurrentStep(uid, construction);

            if (step == null)
            {
                _sawmill.Warning($"Called {nameof(HandleEdge)} on entity {uid} but the current state is not valid for that!");
                return HandleResult.False;
            }

            if (!CheckConditions(uid, edge.Conditions))
                return HandleResult.False;

            if (HandleStep(uid, ev, step, out var user, construction)
                is var handle and (HandleResult.False or HandleResult.DoAfter))
                return handle;

            construction.StepIndex++;

            if (construction.StepIndex >= edge.Steps.Count)
            {
                // Edge finished!
                PerformActions(uid, user, edge.Completed);
                construction.EdgeIndex = null;
                construction.StepIndex = 0;

                // We change the node now.
                ChangeNode(uid, user, edge.Target, true, construction);
            }

            return HandleResult.True;
        }

        private HandleResult HandleStep(EntityUid uid, object ev, ConstructionGraphStep step, out EntityUid? user, ConstructionComponent? construction = null)
        {
            user = null;
            if (!Resolve(uid, ref construction))
                return HandleResult.False;

            if (HandleInteraction(uid, ev, step, out user, construction)
                is var handle and (HandleResult.False or HandleResult.DoAfter))
                return handle;

            PerformActions(uid, user, step.Completed);

            return HandleResult.True;
        }

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
                    construction.WaitingDoAfter = false;
                    break;
                }

                case ConstructionDoAfterCancelled cancelled:
                {
                    // DoAfter failed!
                    ev = cancelled.WrappedEvent;
                    doAfterState = DoAfterState.Cancelled;
                    construction.WaitingDoAfter = false;
                    break;
                }
            }

            // Can't perform any interactions while we're waiting for a DoAfter...
            if (construction.WaitingDoAfter)
                return HandleResult.False;

            switch (step)
            {
                case EntityInsertConstructionGraphStep insertStep:
                {
                    if (ev is not InteractUsingEvent interactUsing)
                        break;

                    // TODO: Sanity checks.

                    if (doAfterState == DoAfterState.Cancelled)
                        return HandleResult.False;

                    var insert = interactUsing.Used;
                    if(!insertStep.EntityValid(insert))
                        return HandleResult.False;

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
                                BroadcastFinishedEvent = new ConstructionDoAfterComplete(ev),
                                BroadcastCancelledEvent = new ConstructionDoAfterCancelled(ev)
                            });

                        return HandleResult.DoAfter;
                    }

                    if (insertStep is MaterialConstructionGraphStep materialInsertStep)
                    {
                        if (_stackSystem.Split(insert.Uid, materialInsertStep.Amount, interactUsing.User.Transform.Coordinates) is not { } stack)
                            return HandleResult.False;

                        insert = stack;
                    }

                    if (string.IsNullOrEmpty(insertStep.Store))
                    {
                        insert.Delete();
                    }
                    else
                    {
                        var store = insertStep.Store;
                        construction.Containers.Add(store);
                        _containerSystem.EnsureContainer<Container>(uid, store)
                            .Insert(insert);
                    }

                    return HandleResult.True;
                }

                case ToolConstructionGraphStep toolInsertStep:
                {
                    if (ev is not InteractUsingEvent interactUsing)
                        break;

                    // TODO: Sanity checks.

                    user = interactUsing.User.Uid;

                    if (doAfterState != DoAfterState.None)
                        return doAfterState == DoAfterState.Completed ? HandleResult.True : HandleResult.False;

                    if (!_toolSystem.UseTool(interactUsing.Used.Uid, interactUsing.User.Uid,
                        uid, toolInsertStep.Fuel, toolInsertStep.DoAfter, toolInsertStep.Tool, uid,
                        new ConstructionDoAfterComplete(ev), new ConstructionDoAfterCancelled(ev)))
                        return HandleResult.False;

                    // In the case we're not waiting for a doAfter, then this step is complete!
                    if (toolInsertStep.DoAfter <= 0)
                        return HandleResult.True;

                    construction.WaitingDoAfter = true;
                    return HandleResult.DoAfter;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(step),
                        "You need to code your ConstructionGraphStep behavior by adding a case to the switch.");
            }

            return HandleResult.False;
        }

        private bool CheckConditions(EntityUid uid, IEnumerable<IGraphCondition> conditions)
        {
            var entity = EntityManager.GetEntity(uid);

            foreach (var condition in conditions)
            {
                if (!condition.Condition(entity))
                    return false;
            }

            return true;
        }

        private void PerformActions(EntityUid uid, EntityUid? userUid, IEnumerable<IGraphAction> actions)
        {
            var entity = EntityManager.GetEntity(uid);
            var userEntity = userUid is {} user ? EntityManager.GetEntity(user) : null;

            foreach (var action in actions)
            {
                if (entity.Deleted)
                    break;

                // TODO: Change this signature, make it not use IEntity...
                action.PerformAction(entity, userEntity);
            }
        }

        private void UpdateSteps()
        {
            foreach (var construction in EntityManager.EntityQuery<ConstructionComponent>())
            {
                var uid = construction.Owner.Uid;

                while (construction.InteractionQueue.TryDequeue(out var interaction))
                {
                    HandleEvent(uid, interaction, construction);
                }
            }
        }

        #region Event Handlers

        private void EnqueueEvent(EntityUid uid, ConstructionComponent construction, object args)
        {
            if (args is HandledEntityEventArgs handled)
            {
                if (handled.Handled)
                    return;

                handled.Handled = true;
            }

            construction.InteractionQueue.Enqueue(args);
        }

        #endregion

        private class ConstructionDoAfterComplete : EntityEventArgs
        {
            public readonly object WrappedEvent;
            public readonly object? CustomData;

            public ConstructionDoAfterComplete(object wrappedEvent, object? customData = null)
            {
                WrappedEvent = wrappedEvent;
                CustomData = customData;
            }
        }

        private class ConstructionDoAfterCancelled : EntityEventArgs
        {
            public readonly object WrappedEvent;
            public readonly object? CustomData;

            public ConstructionDoAfterCancelled(object wrappedEvent, object? customData = null)
            {
                WrappedEvent = wrappedEvent;
                CustomData = customData;
            }
        }

        /// <summary>
        ///     Specifies the DoAfter status for a construction event handler.
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
            ///     The step wasn't handled.
            /// </summary>
            False,

            /// <summary>
            ///     The step was handled successfully.
            /// </summary>
            True,

            /// <summary>
            ///     The step is waiting on a DoAfter.
            /// </summary>
            DoAfter,
        }
    }
}
