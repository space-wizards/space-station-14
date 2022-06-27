using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.Roles
{
    [UsedImplicitly]
    public sealed class OverallPlaytimeRequirement : JobRequirement
    {
        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;

        public override Tuple<bool, string?> GetRequirementStatus(NetUserId id)
        {
            return new Tuple<bool, string?>(true, "not coded yet");
        }
    }
}
