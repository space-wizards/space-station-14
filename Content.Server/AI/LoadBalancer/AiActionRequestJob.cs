using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.AI.Components;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Utility;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.AI;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.AI.LoadBalancer
{
    public sealed class AiActionRequestJob : Job<UtilityAction>
    {
#if DEBUG
        public static event Action<SharedAiDebug.UtilityAiDebugMessage>? FoundAction;
#endif
        private readonly AiActionRequest _request;

        public AiActionRequestJob(
            double maxTime,
            AiActionRequest request,
            CancellationToken cancellationToken = default) : base(maxTime, cancellationToken)
        {
            _request = request;
        }

        protected override async Task<UtilityAction?> Process()
        {
            if (_request.Context == null)
            {
                return null;
            }

            var entity = _request.Context.GetState<SelfState>().GetValue();

            if (!IoCManager.Resolve<IEntityManager>().HasComponent<AiControllerComponent>(entity))
            {
                return null;
            }

            if (_request.Actions == null || _request.Context == null)
            {
                return null;
            }

            var consideredTaskCount = 0;
            // Actions are pre-sorted
            var actions = new Stack<IAiUtility>(_request.Actions);

            // So essentially we go through and once we have a valid score that score becomes the cutoff;
            // once the bonus of new tasks is below the cutoff we can stop evaluating.

            // Use last action as the basis for the cutoff
            var cutoff = _request.Context.GetState<LastUtilityScoreState>().GetValue();
            UtilityAction? foundAction = null;

            // To see what I was trying to do watch these 2 videos about Infinite Axis Utility System (IAUS):
            // Architecture Tricks: Managing Behaviors in Time, Space, and Depth
            // Building a Better Centaur

            // We'll want to cap the considered entities at some point, e.g. if 500 guns are in a stack cap it at 256 or whatever
            while (actions.Count > 0)
            {
                if (consideredTaskCount > 0 && consideredTaskCount % 5 == 0)
                {
                    await SuspendIfOutOfTime();

                    // If this happens then that means something changed when we resumed so ABORT
                    if (actions.Count == 0 || _request.Context == null)
                    {
                        return null;
                    }
                }

                var action = actions.Pop();
                switch (action)
                {
                    case ExpandableUtilityAction expandableUtilityAction:
                        if (!expandableUtilityAction.IsValid(_request.Context))
                        {
                            break;
                        }

                        foreach (var expanded in expandableUtilityAction.GetActions(_request.Context))
                        {
                            actions.Push(expanded);
                        }
                        break;
                    case UtilityAction utilityAction:
                        consideredTaskCount++;
                        var bonus = utilityAction.Bonus;

                        if (bonus < cutoff)
                        {
                            // We know none of the other actions can beat this as they're pre-sorted
                            actions.Clear();
                            break;
                        }

                        var score = utilityAction.GetScore(_request.Context, cutoff);
                        if (score > cutoff)
                        {
                            foundAction = utilityAction;
                            cutoff = score;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _request.Context.GetState<LastUtilityScoreState>().SetValue(cutoff);
#if DEBUG
            if (foundAction != null)
            {
                var selfState = _request.Context.GetState<SelfState>().GetValue();

                DebugTools.AssertNotNull(selfState);

                FoundAction?.Invoke(new SharedAiDebug.UtilityAiDebugMessage(
                    selfState!,
                    DebugTime,
                    cutoff,
                    foundAction.GetType().Name,
                    consideredTaskCount));
            }

#endif
            _request.Context.ResetPlanning();

            return foundAction;
        }
    }
}
