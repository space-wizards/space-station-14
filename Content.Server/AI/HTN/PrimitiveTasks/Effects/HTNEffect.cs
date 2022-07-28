namespace Content.Server.AI.HTN.PrimitiveTasks;

[ImplicitDataDefinitionForInheritors]
public abstract class HTNEffect
{
    public abstract void Effect(Dictionary<string, object> blackboard);
}
