using Content.Server.RoleTimers;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
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

        public override bool RequirementFulfilled(NetUserId id)
        {
            var mgr = IoCManager.Resolve<RoleTimerManager>();
            var playtime = mgr.GetPlayTimeForRole(id, Role);
            return playtime >= Time;
        }
    }
}
