namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Condition that needs to be true for a particular primitive task or compound task branch.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class HTNPrecondition
{
    /// <summary>
    /// Handles one-time initialization of this precondition.
    /// </summary>
    public virtual void Initialize(IDependencyCollection deps)
    {
        deps.InjectDependencies(this);
    }

    /// <summary>
    /// Has this precondition been met for planning purposes?
    /// </summary>
    public abstract bool IsMet(NPCBlackboard blackboard);
}
