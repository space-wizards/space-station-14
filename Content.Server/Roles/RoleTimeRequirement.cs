using Content.Server.RoleTimers;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Roles
{
    [UsedImplicitly]
    public sealed class RoleTimeRequirement : JobRequirement
    {
        [DataField("role")]
        public string Role = default!;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;

        public override Tuple<bool, string?> GetRequirementStatus(NetUserId id)
        {
            var mgr = IoCManager.Resolve<RoleTimerManager>();
            var playtime = mgr.GetPlayTimeForRole(id, Role) ?? TimeSpan.Zero;
            return new Tuple<bool, string?>(playtime >= Time,
                Loc.GetString("job-requirement-time-remaining",
                    // TODO: Improve the readability of the time value (30 minutes instead of 0.5 hours and such)
                    ("duration", Time.Subtract(playtime).TotalHours),
                    ("units", "hours"),
                    ("requirement", Role)));
        }
    }
}
