using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
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

    public bool TryRequirementMet(NetUserId id, JobPrototype job,
        ref TimeSpan? overallTime,
        ref Dictionary<string, TimeSpan>? roleTimes,
        [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        if (job.Requirements == null) return true;

        foreach (var requirement in job.Requirements)
        {
            if (!TryRequirementMet(id, job, requirement, ref overallTime, ref roleTimes, out reason)) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a string with the reason why a particular requirement may not be met.
    /// </summary>
    public bool TryRequirementMet(NetUserId id, JobPrototype job, JobRequirement requirement,
        ref TimeSpan? overallTime,
        ref Dictionary<string, TimeSpan>? roleTimes,
        [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        switch (requirement)
        {
            case DepartmentTimeRequirement deptRequirement:
                roleTimes ??= GetRolePlaytimes(id);
                var playtime = TimeSpan.Zero;

                // Check all jobs' departments
                var jobs = ProtoManager.Index<DepartmentPrototype>(deptRequirement.Department).Roles.ToHashSet();

                // Check all jobs' playtime
                foreach (var other in jobs)
                {
                    roleTimes.TryGetValue(other, out var otherTime);
                    playtime += otherTime;
                }

                var deptDiff = deptRequirement.Time.TotalMinutes - playtime.TotalMinutes;

                if (deptDiff <= 0) return true;

                reason = Loc.GetString(
                    "role-timer-department-insufficient",
                    ("time", $"{deptDiff:0}"),
                    ("department", Loc.GetString(deptRequirement.Department)));
                return false;

            case OverallPlaytimeRequirement overallRequirement:
                overallTime ??= GetOverallPlaytime(id);
                var overallDiff = overallRequirement.Time.TotalMinutes - overallTime.Value.TotalMinutes;

                if (overallDiff <= 0 || overallTime.Value >= overallRequirement.Time) return true;

                reason = Loc.GetString("role-timer-overall-insufficient", ("time", $"{overallDiff:0}"));
                return false;

            case RoleTimeRequirement roleRequirement:
                roleTimes ??= GetRolePlaytimes(id);
                roleTimes.TryGetValue(roleRequirement.Role, out var roleTime);
                var roleDiff = roleRequirement.Time.TotalMinutes - roleTime.TotalMinutes;

                if (roleDiff <= 0) return true;

                reason = Loc.GetString(
                    "role-timer-role-insufficient",
                    ("time", $"{roleDiff:0}"),
                    ("job", ProtoManager.Index<JobPrototype>(roleRequirement.Role).LocalizedName));
                return false;
            default:
                throw new NotImplementedException();
        }
    }

    protected abstract TimeSpan GetOverallPlaytime(NetUserId id);
    protected abstract Dictionary<string, TimeSpan> GetRolePlaytimes(NetUserId id);
}
