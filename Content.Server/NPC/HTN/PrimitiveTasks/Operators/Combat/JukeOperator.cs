namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed class JukeOperator : HTNOperator
{
    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // TODO:
        return HTNOperatorStatus.Finished;
    }

    public enum JukeType : byte
    {
        AdjacentTile
    }
}
