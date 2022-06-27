using Content.Server.RoleTimers;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
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

        public override bool RequirementFulfilled(NetUserId id)
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

            return playtime >= Time;
        }
    }
}
