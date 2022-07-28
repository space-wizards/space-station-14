using System.Threading.Tasks;

namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class MoveToOperator : HTNOperator
{
    [ViewVariables, DataField("pathfindInPlanning")]
    public bool PathfindInPlanning = true;

    [ViewVariables, DataField("key")]
    public string TargetKey = "MovementTarget";

    public override async Task PlanUpdate(NPCBlackboard blackboard)
    {
        if (!PathfindInPlanning)
            return;

        // TODO: Try pathfinding
        return;
    }
}
