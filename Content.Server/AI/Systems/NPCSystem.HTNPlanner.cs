using Content.Server.AI.HTN;
using Content.Server.AI.HTN.PrimitiveTasks;
using Robust.Shared.Utility;

namespace Content.Server.AI.Systems;

public sealed partial class NPCSystem
{
    private HTNPlan GetPlan(HTNComponent component)
    {
        return GetPlan(component.RootTask, component.BlackboardA.ShallowClone());
    }

    private HTNPlan GetPlan(HTNCompoundTask rootTask, Dictionary<string, object> blackboard)
    {
        /*
         * Really the best reference for what a HTN looks like is http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
         * It's kinda like a behaviour tree but also can consider multiple actions in sequence.
         *
         * Methods have been renamed to branches
         */

        var decompHistory = new Stack<DecompositionState>();

        // method traversal record. Whenever we find a new compound task this updates.
        var mtrIndex = 0;

        // TODO: Need to store method traversal record
        // This is so we can know if a new plan is better than an old plan.

        var tasksToProcess = new Stack<HTNTask>();
        var finalPlan = new List<HTNPrimitiveTask>();
        tasksToProcess.Push(rootTask);

        // How many primitive tasks we've added since last record.
        var primitiveCount = 0;

        while (tasksToProcess.TryPop(out var currentTask))
        {
            switch (currentTask)
            {
                case HTNCompoundTask compound:
                    if (TryFindSatisfiedMethod(compound, tasksToProcess, blackboard, ref mtrIndex))
                    {
                        // Need to copy worldstate to roll it back
                        // Don't need to copy taskstoprocess as we can just clear it and set it to the compound task we roll back to.
                        // Don't need to copy finalplan as we can just count how many primitives we've added since last record

                        decompHistory.Push(new DecompositionState()
                        {
                            Blackboard = blackboard.ShallowClone(),
                            CompoundTask = compound,
                            MethodTraversal = mtrIndex,
                            PrimitiveCount = primitiveCount,
                        });

                        primitiveCount = 0;
                        // Reset method traversal
                        mtrIndex = 0;
                    }
                    else
                    {
                        RestoreTolastDecomposedTask(decompHistory, tasksToProcess, finalPlan, ref blackboard,
                            ref mtrIndex);
                    }
                    break;
                case HTNPrimitiveTask primitive:
                    if (PrimitiveConditionMet(primitive, blackboard))
                    {
                        primitiveCount++;
                        finalPlan.Add(primitive);

                        foreach (var effect in primitive.Effects)
                        {
                            effect.Effect(blackboard);
                        }
                    }
                    else
                    {
                        RestoreTolastDecomposedTask(decompHistory, tasksToProcess, finalPlan, ref blackboard, ref mtrIndex);
                    }

                    break;
            }
        }

        return new HTNPlan(finalPlan);
    }

    private bool PrimitiveConditionMet(HTNPrimitiveTask primitive, Dictionary<string, object> blackboard)
    {
        foreach (var con in primitive.Preconditions)
        {
            if (con.IsMet(blackboard)) continue;
            return false;
        }

        return true;
    }

    private bool TryFindSatisfiedMethod(HTNCompoundTask compound, Stack<HTNTask> tasksToProcess, Dictionary<string, object> blackboard, ref int mtrIndex)
    {
        for (var i = mtrIndex; i < compound.Branches.Count; i++)
        {
            var branch = compound.Branches[i];
            var isValid = true;

            foreach (var con in branch.Preconditions)
            {
                if (con.IsMet(blackboard)) continue;
                isValid = false;
                break;
            }

            if (!isValid) continue;

            foreach (var task in branch.Tasks)
            {
                tasksToProcess.Push(task);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Restores the planner state.
    /// </summary>
    private void RestoreTolastDecomposedTask(Stack<DecompositionState> decompHistory,
        Stack<HTNTask> tasksToProcess,
        List<HTNPrimitiveTask> finalPlan,
        ref Dictionary<string, object> blackboard,
        ref int mtrIndex)
    {
        tasksToProcess.Clear();

        // No plan found so this will just break normally.
        if (!decompHistory.TryPop(out var lastDecomp))
            return;

        // Increment MTR so next time we try the next method on the compound task.
        mtrIndex = lastDecomp.MethodTraversal++;

        // Final plan only has primitive tasks added to it so we can just remove the count we've tracked since the last decomp.
        finalPlan.RemoveRange(finalPlan.Count - lastDecomp.PrimitiveCount, lastDecomp.PrimitiveCount);

        blackboard = lastDecomp.Blackboard;
        tasksToProcess.Push(lastDecomp.CompoundTask);
    }

    private sealed class DecompositionState
    {
        /// <summary>
        /// Blackboard as at decomposition.
        /// </summary>
        public Dictionary<string, object> Blackboard = default!;

        /// <summary>
        /// How many primitive tasks we've added since last decompositionstate.
        /// </summary>
        public int PrimitiveCount;

        /// <summary>
        /// The compound task that owns this decomposition.
        /// </summary>
        public HTNCompoundTask CompoundTask = default!;

        /// <summary>
        /// Which method (AKA branch) we took of the compound task. Whenever we rollback the decomposition state
        /// this gets incremented by 1 so we check the next method.
        /// </summary>
        public int MethodTraversal;
    }
}
