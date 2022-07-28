namespace Content.Server.AI.HTN;

public abstract class HTNPrecondition
{
    public abstract bool IsMet(Dictionary<string, object> blackboard);
}
