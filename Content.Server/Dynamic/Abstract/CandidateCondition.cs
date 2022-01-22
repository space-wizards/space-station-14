using Robust.Shared.GameObjects;

namespace Content.Server.Dynamic.Abstract;

/// <summary>
///     Every single possible candidate is passed in through each condition, to determine
///     whether they are valid as a candidate.
/// </summary>
public abstract class CandidateCondition
{
    public abstract bool Condition(Candidate candidate, IEntityManager entityManager);
}
