using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.CPUJob.JobQueues;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.NPC.HTN;

/// <summary>
/// A time-sliced job that will retrieve an HTN plan eventually.
/// </summary>
public sealed class HTNPlanJob : Job<HTNPlan>
{
    private readonly HTNSystem _htn;
    private readonly HTNTask _rootTask;
    private NPCBlackboard _blackboard;

    private IPrototypeManager _protoManager;

    /// <summary>
    /// Branch traversal of an existing plan (if applicable).
    /// </summary>
    private List<int>? _branchTraversal;

    public HTNPlanJob(
        double maxTime,
        IPrototypeManager protoManager,
        HTNSystem htn,
        HTNTask rootTask,
        NPCBlackboard blackboard,
        List<int>? branchTraversal,
        CancellationToken cancellationToken = default) : base(maxTime, cancellationToken)
    {
        _protoManager = protoManager;
        _htn = htn;
        _rootTask = rootTask;
        _blackboard = blackboard;
        _branchTraversal = branchTraversal;
    }

    protected override async Task<HTNPlan?> Process()
    {
        /*
         * Really the best reference for what a HTN looks like is http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
         * It's kinda like a behaviour tree but also can consider multiple actions in sequence.
         *
         * Methods have been renamed to branches
         */

        var decompHistory = new Stack<DecompositionState>();

        // branch traversal record. Whenever we find a new compound task this updates.
        var btrIndex = 0;
        var btr = new List<int>();

        // For some tasks we may do something expensive or want to re-use the planning result.
        // e.g. pathfind to a target before deciding to attack it.
        // Given all of the primitive tasks are singletons we need to store the data somewhere
        // hence we'll store it here.
        var appliedStates = new List<Dictionary<string, object>?>();

        var tasksToProcess = new Queue<HTNTask>();
        var finalPlanCount = 0;
        tasksToProcess.Enqueue(_rootTask);

        // How many primitive tasks we've added since last record.
        var primitiveCount = 0;

        while (tasksToProcess.TryDequeue(out var currentTask))
        {
            switch (currentTask)
            {
                case HTNCompoundTask compound:
                    await SuspendIfOutOfTime();

                    if (TryFindSatisfiedMethod(compound, tasksToProcess, _blackboard, ref btrIndex))
                    {
                        // Need to copy worldstate to roll it back
                        // Don't need to copy taskstoprocess as we can just clear it and set it to the compound task we roll back to.
                        // Don't need to copy finalplan as we can just count how many primitives we've added since last record

                        decompHistory.Push(new DecompositionState()
                        {
                            Blackboard = _blackboard.ShallowClone(),
                            CompoundTask = compound,
                            BranchTraversal = btrIndex,
                            PrimitiveCount = primitiveCount,
                        });

                        btr.Add(btrIndex);

                        // TODO: Early out if existing plan is better and save lots of time.
                        // my brain is not working rn AAA

                        primitiveCount = 0;
                        // Reset method traversal
                        btrIndex = 0;
                    }
                    else
                    {
                        RestoreTolastDecomposedTask(decompHistory, tasksToProcess, appliedStates, ref finalPlanCount, ref primitiveCount, ref _blackboard, ref btrIndex, ref btr);
                    }
                    break;
                case HTNParallelTask parallel:
                    // Treated similar to a compound.
                    await SuspendIfOutOfTime();

                    // Push all of them to taskstoprocess
                    if (PushTasks(parallel.Tasks, parallel.Preconditions, tasksToProcess, _blackboard, ref btrIndex))
                    {
                        decompHistory.Push(new DecompositionState()
                        {
                            Blackboard = _blackboard.ShallowClone(),
                            CompoundTask = parallel,
                            BranchTraversal = btrIndex,
                            PrimitiveCount = primitiveCount,
                        });

                        btr.Add(btrIndex);
                        primitiveCount = 0;
                        // Reset method traversal
                        btrIndex = 0;
                    }

                    break;
                case HTNRepeatingTask repeating:
                    // Also treated similar to a compound.
                    await SuspendIfOutOfTime();

                    // Push all of them to taskstoprocess
                    if (PushTasks(repeating.Tasks, repeating.Preconditions, tasksToProcess, _blackboard, ref btrIndex))
                    {
                        decompHistory.Push(new DecompositionState()
                        {
                            Blackboard = _blackboard.ShallowClone(),
                            CompoundTask = repeating,
                            BranchTraversal = btrIndex,
                            PrimitiveCount = primitiveCount,
                        });

                        btr.Add(btrIndex);
                        primitiveCount = 0;
                        // Reset method traversal
                        btrIndex = 0;
                    }

                    break;

                case HTNPrimitiveTask primitive:
                    if (await WaitAsyncTask(PrimitiveConditionMet(primitive, _blackboard, appliedStates)))
                    {
                        primitiveCount++;
                        finalPlanCount++;
                    }
                    else
                    {
                        RestoreTolastDecomposedTask(decompHistory, tasksToProcess, appliedStates, ref finalPlanCount, ref primitiveCount, ref _blackboard, ref btrIndex, ref btr);
                    }

                    break;
            }
        }

        if (finalPlanCount == 0)
        {
            return null;
        }

        var branchTraversalRecord = decompHistory.Reverse().Select(o => o.BranchTraversal).ToList();

        var plan = new List<HTNTask>();
        var tasks = new Queue<HTNTask>();
        tasks.Enqueue(_rootTask);
        var finalBtrIndex = 0;

        // Reconstruct the plan
        // Compound tasks get decomposed, while other tasks get added as is.
        while (tasks.TryDequeue(out var task))
        {
            switch (task)
            {
                // TODO: Need to make sure on the plan that teh compounds inside parallel etc are decomposed properly.
                case HTNCompoundTask compound:
                    var compBranch = _protoManager.Index<HTNCompoundPrototype>(compound.Task).Branches[branchTraversalRecord[finalBtrIndex]];

                    foreach (var sub in compBranch.Tasks)
                    {
                        tasks.Enqueue(sub);
                    }

                    finalBtrIndex++;

                    break;
                case HTNParallelTask parallel:
                    DebugTools.Assert(finalBtrIndex == 0);
                    tasks.Enqueue(parallel);
                    break;
                case HTNRepeatingTask repeating:
                    DebugTools.Assert(finalBtrIndex == 0);
                    tasks.Enqueue(repeating);
                    break;
                case HTNPrimitiveTask primitive:
                    DebugTools.Assert(finalBtrIndex == 0);
                    tasks.Enqueue(primitive);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return new HTNPlan(plan, branchTraversalRecord, appliedStates);
    }

    private void AddFinalTasks(HTNTask task, int branch, List<HTNTask> tasks)
    {

    }

    private async Task<bool> PrimitiveConditionMet(HTNPrimitiveTask primitive, NPCBlackboard blackboard, List<Dictionary<string, object>?> appliedStates)
    {
        blackboard.ReadOnly = true;

        foreach (var con in primitive.Preconditions)
        {
            if (con.IsMet(blackboard))
                continue;

            return false;
        }

        var (valid, effects) = await primitive.Operator.Plan(blackboard, Cancellation);

        if (!valid)
            return false;

        blackboard.ReadOnly = false;

        if (effects != null)
        {
            foreach (var (key, value) in effects)
            {
                blackboard.SetValue(key, value);
            }
        }

        appliedStates.Add(effects);

        return true;
    }

    /// <summary>
    /// Goes through each compound task branch and tries to find an appropriate one.
    /// </summary>
    private bool TryFindSatisfiedMethod(HTNCompoundTask compoundId, Queue<HTNTask> tasksToProcess, NPCBlackboard blackboard, ref int mtrIndex)
    {
        var compound = _protoManager.Index<HTNCompoundPrototype>(compoundId.Task);
        var compBranches = _htn.CompoundBranches[compound];

        for (var i = mtrIndex; i < compound.Branches.Count; i++)
        {
            var branch = compound.Branches[i];
            var isValid = true;

            foreach (var con in branch.Preconditions)
            {
                if (con.IsMet(blackboard))
                    continue;

                isValid = false;
                break;
            }

            if (!isValid)
                continue;

            var branchTasks = compBranches[i];

            foreach (var task in branchTasks)
            {
                tasksToProcess.Enqueue(task);
            }

            return true;
        }

        return false;
    }

    private bool PushTasks(List<HTNTask> tasks, List<HTNPrecondition> preconditions, Queue<HTNTask> tasksToProcess, NPCBlackboard blackboard,
        ref int mtrIndex)
    {
        foreach (var con in preconditions)
        {
            if (con.IsMet(blackboard))
                continue;

            return false;
        }

        foreach (var task in tasks)
        {
            tasksToProcess.Enqueue(task);
        }

        return true;
    }

    /// <summary>
    /// Restores the planner state.
    /// </summary>
    private void RestoreTolastDecomposedTask(
        Stack<DecompositionState> decompHistory,
        Queue<HTNTask> tasksToProcess,
        List<Dictionary<string, object>?> appliedStates,
        ref int finalPlan,
        ref int primitiveCount,
        ref NPCBlackboard blackboard,
        ref int mtrIndex,
        ref List<int> btr)
    {
        tasksToProcess.Clear();

        // No plan found so this will just break normally.
        if (!decompHistory.TryPop(out var lastDecomp))
            return;

        // Increment MTR so next time we try the next method on the compound task.
        mtrIndex = lastDecomp.BranchTraversal + 1;

        // Final plan only has primitive tasks added to it so we can just remove the count we've tracked since the last decomp.
        finalPlan -= primitiveCount;
        appliedStates.RemoveRange(finalPlan, primitiveCount);
        btr.RemoveRange(finalPlan, primitiveCount);

        primitiveCount = lastDecomp.PrimitiveCount;
        blackboard = lastDecomp.Blackboard;
        tasksToProcess.Enqueue((HTNTask) lastDecomp.CompoundTask);
    }

    /// <summary>
    /// Stores the state of an HTN Plan while planning it. This is so we can rollback if a particular branch is unsuitable.
    /// </summary>
    private sealed class DecompositionState
    {
        /// <summary>
        /// Blackboard as at decomposition.
        /// </summary>
        public NPCBlackboard Blackboard = default!;

        /// <summary>
        /// How many primitive tasks we've added since last decompositionstate.
        /// </summary>
        public int PrimitiveCount;

        /// <summary>
        /// The task that owns this decomposition.
        /// </summary>
        public IHTNCompound CompoundTask = default!;

        // This may not be necessary for planning but may be useful for debugging so I didn't remove it.
        /// <summary>
        /// Which branch (AKA method) we took of the compound task. Whenever we rollback the decomposition state
        /// this gets incremented by 1 so we check the next method.
        /// </summary>
        public int BranchTraversal;
    }
}
