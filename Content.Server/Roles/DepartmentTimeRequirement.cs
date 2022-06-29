using Content.Server.RoleTimers;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles
{
    [UsedImplicitly]
    public sealed class DepartmentTimeRequirement : JobRequirement
    {
        [DataField("department")]
        public string Department = default!;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;

        public override ValueTuple<bool, string?> GetRequirementStatus(NetUserId id)
        {
            var mgr = IoCManager.Resolve<RoleTimerManager>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<JobPrototype>();
            var jobs = new HashSet<JobPrototype>();
            var playtime = TimeSpan.Zero;

            // Check all jobs' departments
            foreach (var prototype in prototypes)
            {
                foreach (var dept in prototype.Departments)
                {
                    if (dept != Department) continue;
                    jobs.Add(prototype);
                    break;
                }
            }

            // Check all jobs' playtime
            foreach (var job in jobs)
            {
                var time = mgr.GetPlayTimeForRole(id, job.ID);
                if(time == null) continue;
                playtime += time.Value;
            }

            return new ValueTuple<bool, string?>(playtime >= Time,
                Loc.GetString("job-requirement-time-remaining",
                    // TODO: Improve the readability of the time value (30 minutes instead of 0.5 hours and such)
                    ("duration", Time.Subtract(playtime).TotalHours),
                    ("units", "hours"),
                    ("requirement", Department)));
        }
    }
}
