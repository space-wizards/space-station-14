namespace Content.Server.AI.HTN;

[ImplicitDataDefinitionForInheritors]
public abstract class HTNPrecondition
{
    public abstract bool IsMet(Dictionary<string, object> blackboard);
}
