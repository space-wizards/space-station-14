using Robust.Shared.Configuration;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

public abstract class SharedRoleTimerSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager ConfigManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("roletimers");
    }

    /// <summary>
    /// Returns a string with the reason why a particular requirement may not be met.
    /// </summary>
    public string? RequirementMet(ICommonSession session, JobPrototype job, JobRequirement requirement,
        TimeSpan? overallTime = null,
        Dictionary<string, TimeSpan>? roleTimes = null)
    {
        switch (requirement)
        {
            case DepartmentTimeRequirement deptRequirement:
                roleTimes ??= GetRolePlaytime(session, job.ID);
                var jobs = new HashSet<JobPrototype>();
                var playtime = TimeSpan.Zero;

                // Check all jobs' departments
                foreach (var prototype in ProtoManager.EnumeratePrototypes<JobPrototype>())
                {
                    foreach (var dept in prototype.Departments)
                    {
                        if (dept != deptRequirement.Department) continue;
                        jobs.Add(prototype);
                        break;
                    }
                }

                // Check all jobs' playtime
                foreach (var other in jobs)
                {
                    roleTimes.TryGetValue(other.ID, out var otherTime);
                    playtime += otherTime;
                }

                var deptDiff = deptRequirement.Time.TotalMinutes - playtime.TotalMinutes;

                if (deptDiff <= 0) return null;

                return Loc.GetString(
                    "role-timer-department-insufficient",
                    ("time", deptDiff),
                    ("department", Loc.GetString(deptRequirement.Department)));

            case OverallPlaytimeRequirement overallRequirement:
                overallTime ??= GetOverallPlaytime(session);
                var overallDiff = overallRequirement.Time.TotalMinutes - overallTime.Value.TotalMinutes;

                if (overallDiff <= 0) return null;

                return overallTime.Value >= overallRequirement.Time ? null : Loc.GetString("role-timer-overall-insufficient", ("time", overallDiff));

            case RoleTimeRequirement roleRequirement:
                roleTimes ??= GetRolePlaytime(session, job.ID);
                roleTimes.TryGetValue(roleRequirement.Role, out var roleTime);
                var roleDiff = roleRequirement.Time.TotalMinutes - roleTime.TotalMinutes;

                if (roleDiff <= 0) return null;

                return Loc.GetString(
                    "role-timer-role-insufficient",
                    ("time", roleDiff),
                    ("job", ProtoManager.Index<JobPrototype>(roleRequirement.Role).LocalizedName));
            default:
                throw new NotImplementedException();
        }

        return null;
    }

    protected abstract TimeSpan GetOverallPlaytime(ICommonSession session);
    protected abstract Dictionary<string, TimeSpan> GetRolePlaytime(ICommonSession session, string role);
}
