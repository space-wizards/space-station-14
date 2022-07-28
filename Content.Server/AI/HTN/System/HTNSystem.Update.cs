using Content.Server.AI.HTN.PrimitiveTasks;

namespace Content.Server.AI.HTN;

public sealed partial class HTNSystem : EntitySystem
{
    private HTNOperatorStatus Update(NPCBlackboard blackboard, HTNOperator op, float frameTime)
    {
        switch (op)
        {
            default:
                return HTNOperatorStatus.Finished;
        }
    }

    private enum HTNOperatorStatus
    {
        Startup,
        Continue,
        Finished,
    }
}
