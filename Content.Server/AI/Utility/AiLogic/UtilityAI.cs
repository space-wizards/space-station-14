using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.AI.Operators;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems.AI;
using Content.Server.GameObjects.EntitySystems.AI.LoadBalancer;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.AI.Utility.AiLogic
{
    // TODO: Need to split out the IMover stuff for NPC to a generic one that can be used for hoomans as well.
    [RegisterComponent]
    [ComponentReference(typeof(AiControllerComponent)), ComponentReference(typeof(IMoverComponent))]
    public sealed class UtilityAi : AiControllerComponent, ISerializationHooks
    {
        public override string Name => "UtilityAI";

        // TODO: Look at having ParallelOperators (probably no more than that as then you'd have a full-blown BT)
        // Also RepeatOperators (e.g. if we're following an entity keep repeating MoveToEntity)
        private AiActionSystem _planner = default!;
        public Blackboard Blackboard => _blackboard;
        private Blackboard _blackboard = default!;

        /// <summary>
        ///     The sum of all BehaviorSets gives us what actions the AI can take
        /// </summary>
        [DataField("behaviorSets")]
        public HashSet<string> BehaviorSets { get; } = new();

        public List<IAiUtility> AvailableActions { get; set; } = new();

        /// <summary>
        /// The currently running action; most importantly are the operators.
        /// </summary>
        public UtilityAction? CurrentAction { get; private set; }

        /// <summary>
        /// How frequently we can re-plan. If an AI's in combat you could decrease the cooldown,
        /// or if there's no players nearby increase it.
        /// </summary>
        public float PlanCooldown { get; } = 0.5f;
        private float _planCooldownRemaining;

        /// <summary>
        /// If we've requested a plan then wait patiently for the action
        /// </summary>
        private AiActionRequestJob? _actionRequest;

        private CancellationTokenSource? _actionCancellation;

        /// <summary>
        ///     If we can't do anything then stop thinking; should probably use ActionBlocker instead
        /// </summary>
        private bool _isDead;

        /*public void AfterDeserialization()
        {
            if (BehaviorSets.Count > 0)
            {
                var behaviorManager = IoCManager.Resolve<INpcBehaviorManager>();

                foreach (var bSet in BehaviorSets)
                {
                    behaviorManager.AddBehaviorSet(this, bSet, false);
                }

                behaviorManager.RebuildActions(this);
            }
        }*/

        public override void Initialize()
        {
            if (BehaviorSets.Count > 0)
            {
                var behaviorManager = IoCManager.Resolve<INpcBehaviorManager>();
                behaviorManager.RebuildActions(this);
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SleepAiMessage(this, false));
            }

            base.Initialize();
            _planCooldownRemaining = PlanCooldown;
            _blackboard = new Blackboard(Owner);
            _planner = EntitySystem.Get<AiActionSystem>();
        }

        public override void OnRemove()
        {
            base.OnRemove();
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
            if (_actionRequest == null)
            {
                return;
            }

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
            base.Update(frameTime);

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
                _actionRequest = _planner.RequestAction(new AiActionRequest(Owner.Uid, _blackboard, AvailableActions), _actionCancellation);

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
