using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.AI.Operators;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.BehaviorSets;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Content.Server.GameObjects.EntitySystems.AI;
using Content.Server.GameObjects.EntitySystems.AI.LoadBalancer;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.AI;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server.AI.Utility.AiLogic
{
    public abstract class UtilityAi : AiLogicProcessor
    {
        // TODO: Look at having ParallelOperators (probably no more than that as then you'd have a full-blown BT)
        // Also RepeatOperators (e.g. if we're following an entity keep repeating MoveToEntity)
        private AiActionSystem _planner;
        public Blackboard Blackboard => _blackboard;
        private Blackboard _blackboard;

        /// <summary>
        /// The sum of all BehaviorSets gives us what actions the AI can take
        /// </summary>
        public Dictionary<Type, BehaviorSet> BehaviorSets { get; } = new();
        private readonly List<IAiUtility> _availableActions = new();

        /// <summary>
        /// The currently running action; most importantly are the operators.
        /// </summary>
        public UtilityAction CurrentAction { get; private set; }

        /// <summary>
        /// How frequently we can re-plan. If an AI's in combat you could decrease the cooldown,
        /// or if there's no players nearby increase it.
        /// </summary>
        public float PlanCooldown { get; } = 0.5f;
        private float _planCooldownRemaining;

        /// <summary>
        /// If we've requested a plan then wait patiently for the action
        /// </summary>
        private AiActionRequestJob _actionRequest;

        private CancellationTokenSource _actionCancellation;

        /// <summary>
        /// If we can't do anything then stop thinking; should probably use ActionBlocker instead
        /// </summary>
        private bool _isDead = false;

        // These 2 methods will be used eventually if / when we get a director AI
        public void AddBehaviorSet<T>(T behaviorSet, bool sort = true) where T : BehaviorSet
        {
            if (BehaviorSets.TryAdd(typeof(T), behaviorSet) && sort)
            {
                SortActions();
            }

            if (BehaviorSets.Count == 1 && !EntitySystem.Get<AiSystem>().IsAwake(this))
            {
                IoCManager.Resolve<IEntityManager>()
                    .EventBus
                    .RaiseEvent(EventSource.Local, new SleepAiMessage(this, false));
            }
        }

        public void RemoveBehaviorSet(Type behaviorSet)
        {
            DebugTools.Assert(behaviorSet.IsAssignableFrom(typeof(BehaviorSet)));

            if (BehaviorSets.ContainsKey(behaviorSet))
            {
                BehaviorSets.Remove(behaviorSet);
                SortActions();
            }

            if (BehaviorSets.Count == 0)
            {
                IoCManager.Resolve<IEntityManager>()
                    .EventBus
                    .RaiseEvent(EventSource.Local, new SleepAiMessage(this, true));
            }
        }

        /// <summary>
        /// Whenever the behavior sets are changed we'll re-sort the actions by bonus
        /// </summary>
        protected void SortActions()
        {
            _availableActions.Clear();
            foreach (var set in BehaviorSets.Values)
            {
                foreach (var action in set.Actions)
                {
                    var found = false;

                    for (var i = 0; i < _availableActions.Count; i++)
                    {
                        if (_availableActions[i].Bonus < action.Bonus)
                        {
                            _availableActions.Insert(i, action);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        _availableActions.Add(action);
                    }
                }
            }

            _availableActions.Reverse();
        }

        public override void Setup()
        {
            base.Setup();
            _planCooldownRemaining = PlanCooldown;
            _blackboard = new Blackboard(SelfEntity);
            _planner = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiActionSystem>();
        }

        public override void Shutdown()
        {
            var currentOp = CurrentAction?.ActionOperators.Peek();
            currentOp?.Shutdown(Outcome.Failed);
            CurrentAction?.Shutdown();
            CurrentAction = null;
        }

        public void MobStateChanged(MobStateChangedMessage message)
        {
            var oldDeadState = _isDead;
            _isDead = message.Component.IsIncapacitated();

            if (oldDeadState != _isDead)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();

                switch (_isDead)
                {
                    case true:
                        entityManager.EventBus.RaiseEvent(EventSource.Local, new SleepAiMessage(this, true));
                        break;
                    case false:
                        entityManager.EventBus.RaiseEvent(EventSource.Local, new SleepAiMessage(this, false));
                        break;
                }
            }
        }

        private void ReceivedAction()
        {
            switch (_actionRequest.Exception)
            {
                case null:
                    break;
                default:
                    Logger.FatalS("ai", _actionRequest.Exception.ToString());
                    throw _actionRequest.Exception;
            }
            var action = _actionRequest.Result;
            _actionRequest = null;
            // Actions with lower scores should be implicitly dumped by GetAction
            // If we're not allowed to replace the action with an action of the same type then dump.
            if (action == null || !action.CanOverride && CurrentAction?.GetType() == action.GetType())
            {
                return;
            }

            var currentOp = CurrentAction?.ActionOperators.Peek();
            if (currentOp != null && currentOp.HasStartup)
            {
                currentOp.Shutdown(Outcome.Failed);
            }

            CurrentAction = action;
            action.SetupOperators(_blackboard);
        }

        public override void Update(float frameTime)
        {
            // If we asked for a new action we don't want to dump the existing one.
            if (_actionRequest != null)
            {
                if (_actionRequest.Status != JobStatus.Finished)
                {
                    return;
                }

                ReceivedAction();
                // Do something next tick
                return;
            }

            _planCooldownRemaining -= frameTime;

            // Might find a better action while we're doing one already
            if (_planCooldownRemaining <= 0.0f)
            {
                _planCooldownRemaining = PlanCooldown;
                _actionCancellation = new CancellationTokenSource();
                _actionRequest = _planner.RequestAction(new AiActionRequest(SelfEntity.Uid, _blackboard, _availableActions), _actionCancellation);

                return;
            }

            // When we spawn in we won't get an action for a bit
            if (CurrentAction == null)
            {
                return;
            }

            var outcome = CurrentAction.Execute(frameTime);

            switch (outcome)
            {
                case Outcome.Success:
                    if (CurrentAction.ActionOperators.Count == 0)
                    {
                        CurrentAction.Shutdown();
                        CurrentAction = null;
                        // Nothing to compare new action to
                        _blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                    }
                    break;
                case Outcome.Continuing:
                    break;
                case Outcome.Failed:
                    CurrentAction.Shutdown();
                    CurrentAction = null;
                    _blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
