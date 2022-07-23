using System.Runtime.ExceptionServices;
using System.Threading;
using Content.Server.AI.Components;
using Content.Server.AI.LoadBalancer;
using Content.Server.AI.Operators;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Server.AI.EntitySystems;

public sealed partial class NPCSystem
{
    /*
     * Handles Utility AI, implemented via IAUS
     */

    private readonly NpcActionComparer _comparer = new();

    private Dictionary<string, List<Type>> _behaviorSets = new();

    private readonly AiActionJobQueue _aiRequestQueue = new();

    private void InitializeUtility()
    {
        SubscribeLocalEvent<UtilityNPCComponent, ComponentStartup>(OnUtilityStartup);

        foreach (var bSet in _prototypeManager.EnumeratePrototypes<BehaviorSetPrototype>())
        {
            var actions = new List<Type>();

            foreach (var act in bSet.Actions)
            {
                if (!_reflectionManager.TryLooseGetType(act, out var parsedType) ||
                    !typeof(IAiUtility).IsAssignableFrom(parsedType))
                {
                    _sawmill.Error($"Unable to parse AI action for {act}");
                }
                else
                {
                    actions.Add(parsedType);
                }
            }

            _behaviorSets[bSet.ID] = actions;
        }
    }

    private void OnUtilityStartup(EntityUid uid, UtilityNPCComponent component, ComponentStartup args)
    {
        if (component.BehaviorSets.Count > 0)
        {
            RebuildActions(component);
        }

        component._planCooldownRemaining = component.PlanCooldown;
        component._blackboard = new Blackboard(component.Owner);
    }

    public AiActionRequestJob RequestAction(UtilityNPCComponent component, AiActionRequest request, CancellationTokenSource cancellationToken)
    {
        var job = new AiActionRequestJob(0.002, request, cancellationToken.Token);
        // AI should already know if it shouldn't request again
        _aiRequestQueue.EnqueueJob(job);
        return job;
    }

    private void UpdateUtility(float frameTime)
    {
        foreach (var (_, comp) in EntityQuery<ActiveNPCComponent, UtilityNPCComponent>())
        {
            if (_count >= _maxUpdates) break;

            Update(comp, frameTime);
            _count++;
        }

        _aiRequestQueue.Process();
    }

    private void ReceivedAction(UtilityNPCComponent component)
    {
        if (component._actionRequest == null)
        {
            return;
        }

        switch (component._actionRequest.Exception)
        {
            case null:
                break;
            default:
                _sawmill.Fatal(component._actionRequest.Exception.ToString());
                ExceptionDispatchInfo.Capture(component._actionRequest.Exception).Throw();
                // The code never actually reaches here, because the above throws.
                // This is to tell the compiler that the flow never leaves here.
                throw component._actionRequest.Exception;
        }
        var action = component._actionRequest.Result;
        component._actionRequest = null;
        // Actions with lower scores should be implicitly dumped by GetAction
        // If we're not allowed to replace the action with an action of the same type then dump.
        if (action == null || !action.CanOverride && component.CurrentAction?.GetType() == action.GetType())
        {
            return;
        }

        var currentOp = component.CurrentAction?.ActionOperators.Peek();
        if (currentOp != null && currentOp.HasStartup)
        {
            currentOp.Shutdown(Outcome.Failed);
        }

        component.CurrentAction = action;
        action.SetupOperators(component._blackboard);
    }

    private void Update(UtilityNPCComponent component, float frameTime)
    {
        // If we asked for a new action we don't want to dump the existing one.
        if (component._actionRequest != null)
        {
            if (component._actionRequest.Status != JobStatus.Finished)
            {
                return;
            }

            ReceivedAction(component);
            // Do something next tick
            return;
        }

        component._planCooldownRemaining -= frameTime;

        // Might find a better action while we're doing one already
        if (component._planCooldownRemaining <= 0.0f)
        {
            component._planCooldownRemaining = component.PlanCooldown;
            component._actionCancellation = new CancellationTokenSource();
            component._actionRequest = RequestAction(component, new AiActionRequest(component.Owner, component._blackboard, component.AvailableActions), component._actionCancellation);

            return;
        }

        // When we spawn in we won't get an action for a bit
        if (component.CurrentAction == null)
        {
            return;
        }

        var outcome = component.CurrentAction.Execute(frameTime);

        switch (outcome)
        {
            case Outcome.Success:
                if (component.CurrentAction.ActionOperators.Count == 0)
                {
                    component.CurrentAction.Shutdown();
                    component.CurrentAction = null;
                    // Nothing to compare new action to
                    component._blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                }
                break;
            case Outcome.Continuing:
                break;
            case Outcome.Failed:
                component.CurrentAction.Shutdown();
                component.CurrentAction = null;
                component._blackboard.GetState<LastUtilityScoreState>().SetValue(0.0f);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///     Adds the BehaviorSet to the NPC.
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="behaviorSet"></param>
    /// <param name="rebuild">Set to false if you want to manually rebuild it after bulk updates.</param>
    public void AddBehaviorSet(UtilityNPCComponent npc, string behaviorSet, bool rebuild = true)
    {
        if (!_behaviorSets.ContainsKey(behaviorSet))
        {
            _sawmill.Error($"Tried to add BehaviorSet {behaviorSet} to {npc} but no such BehaviorSet found!");
            return;
        }

        if (!npc.BehaviorSets.Add(behaviorSet))
        {
            _sawmill.Error($"Tried to add BehaviorSet {behaviorSet} to {npc} which already has the BehaviorSet!");
            return;
        }

        if (rebuild)
            RebuildActions(npc);

        if (npc.BehaviorSets.Count == 1 && !IsAwake(npc))
            WakeNPC(npc);
    }

    /// <summary>
    ///     Removes the BehaviorSet from the NPC.
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="behaviorSet"></param>
    /// <param name="rebuild">Set to false if yo uwant to manually rebuild it after bulk updates.</param>
    public void RemoveBehaviorSet(UtilityNPCComponent npc, string behaviorSet, bool rebuild = true)
    {
        if (!_behaviorSets.TryGetValue(behaviorSet, out var actions))
        {
            Logger.Error($"Tried to remove BehaviorSet {behaviorSet} from {npc} but no such BehaviorSet found!");
            return;
        }

        if (!npc.BehaviorSets.Remove(behaviorSet))
        {
            Logger.Error($"Tried to remove BehaviorSet {behaviorSet} from {npc} but it doesn't have that BehaviorSet!");
            return;
        }

        if (rebuild)
            RebuildActions(npc);

        if (npc.BehaviorSets.Count == 0)
            SleepNPC(npc);
    }

    /// <summary>
    ///     Clear our actions and re-instantiate them from our BehaviorSets.
    ///     Will ensure each action is unique.
    /// </summary>
    /// <param name="npc"></param>
    public void RebuildActions(UtilityNPCComponent npc)
    {
        npc.AvailableActions.Clear();
        foreach (var bSet in npc.BehaviorSets)
        {
            foreach (var action in GetActions(bSet))
            {
                if (npc.AvailableActions.Contains(action)) continue;
                // Setup
                action.Owner = npc.Owner;

                // Ad to actions.
                npc.AvailableActions.Add(action);
            }
        }

        SortActions(npc);
    }

    private IEnumerable<IAiUtility> GetActions(string behaviorSet)
    {
        foreach (var action in _behaviorSets[behaviorSet])
        {
            yield return (IAiUtility) _typeFactory.CreateInstance(action);
        }
    }

    /// <summary>
    ///     Whenever the behavior sets are changed we'll re-sort the actions by bonus
    /// </summary>
    private void SortActions(UtilityNPCComponent npc)
    {
        npc.AvailableActions.Sort(_comparer);
    }

    private sealed class NpcActionComparer : Comparer<IAiUtility>
    {
        public override int Compare(IAiUtility? x, IAiUtility? y)
        {
            if (x == null || y == null) return 0;
            return y.Bonus.CompareTo(x.Bonus);
        }
    }
}
