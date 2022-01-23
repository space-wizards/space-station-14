using Content.Server.Dynamic.Abstract;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Conditions.Candidate;

public class HasRoleCondition : CandidateCondition
{
    [DataField("role")]
    public string RoleName = default!;

    /// <summary>
    ///     Are we checking for the role (false) or the absence of the role (true)?
    /// </summary>
    [DataField("invert")]
    public bool Invert;

    public override bool Condition(Dynamic.Candidate candidate, IEntityManager entityManager)
    {
        return candidate.Mind.HasRole(RoleName) ^ Invert;
    }
}
