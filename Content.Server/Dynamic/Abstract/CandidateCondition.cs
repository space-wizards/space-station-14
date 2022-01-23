using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Abstract;

/// <summary>
///     Every single possible candidate is passed in through each condition, to determine
///     whether they are valid as a candidate.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class CandidateCondition
{
    public abstract bool Condition(Candidate candidate, IEntityManager entityManager);
}
