namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Condition that needs to be true for a particular primitive task or branch.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class HTNPrecondition
{
    public abstract bool IsMet(NPCBlackboard blackboard);
}
