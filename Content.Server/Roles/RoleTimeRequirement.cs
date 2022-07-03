using Content.Shared.Roles;
using JetBrains.Annotations;

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
    }
}
