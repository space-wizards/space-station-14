using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
    [UsedImplicitly]
    public sealed class OverallPlaytimeRequirement : JobRequirement
    {
        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;

        public override bool RequirementFulfilled(NetUserId id)
        {
            throw new NotImplementedException();
        }
    }
}
