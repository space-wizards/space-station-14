using Content.Server.AI.HTN;
using Content.Server.AI.HTN.PrimitiveTasks;
using Robust.Shared.Utility;

namespace Content.Server.AI.Systems;

public sealed partial class NPCSystem
{
    private HTNPlan GetPlan(HTNComponent component)
    {
        DebugTools.Assert(component.Plan == null);
        var nodes = new Queue<HTNNode>();
        var plan = new Queue<HTNPrimitiveTask>();

        /*
         * There's 3 parts to a HTN.
         * 1. There's the overall HTN nodes that comprise the entire thing. These are mapped by IDs via compound tasks
         * 2. There's the underlying tasks which can be primitive or compound. These are wrapped in HTN nodes.
         * This means the primitive / compound tasks can be re-used across many graphs and their wrapping node would change.
         *
         * Really the best reference for what a HTN looks like is http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
         */

        var currentBlackboard = component.BlackboardA.ShallowClone();
        var tasksToProcess = new Stack<HTNTask>();
        var finalPlan = new List<HTNPrimitiveTask>();
        tasksToProcess.Push(component.RootTask);

        while (tasksToProcess.TryPop(out var currentTask))
        {
            switch (currentTask)
            {
                case HTNCompoundTask compound:
                    if (TryFindSatisfied(compound))

                    // TODO: Satisfied method
                    // TODO: Insert subtasks to taskstoprocess

                    // TODO: If not satisfied restore to last decomposed
                    break;
                case HTNPrimitiveTask primitive:
                    // TODO: If conditions met
                    finalPlan.Add(primitive);
                    // TODO: Apply effects to world state
                    break;
            }
        }

        // TODO: Make this per task.
        var recursionLimit = 100;
        var recursion = 0;

        while (recursion < recursionLimit)
        {
            var node = nodes.Peek();

            // TODO: Assumed autosucceed for now.
            plan.Enqueue(node.Task);

            if (node.Edges.Count == 0)
                break;

            foreach (var edge in node.Edges)
            {
                nodes.Enqueue(map[edge]);
            }

            recursion++;
        }

        return new HTNPlan(plan.ToArray());
    }

    private void RecordDecompositionOfTask(HTNTask currentTask, List<HTNTask> finalPlan, decompHistory)
    {

    }

    private void

    private HTNTask GetTask(string id)
    {
        if (_prototypeManager.TryIndex<HTNCompoundTask>(id, out var compound))
            return compound;
        else if (_prototypeManager.TryIndex<HTNPrimitiveTask>(id, out var primitive))
            return primitive;

        throw new InvalidOperationException();
    }
}
