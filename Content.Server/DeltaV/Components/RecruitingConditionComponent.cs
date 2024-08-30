using Content.Server.Objectives.Systems;
using Content.Shared.DeltaV.Recruiter;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the recruiter's pen to be used by a number of people to sign paper.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(RecruitingConditionSystem), typeof(SharedRecruiterPenSystem))]
public sealed partial class RecruitingConditionComponent : Component
{
    [DataField]
    public int Recruited;
}
