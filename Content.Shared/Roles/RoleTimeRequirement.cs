using JetBrains.Annotations;

namespace Content.Shared.Roles
{
    [UsedImplicitly]
    public sealed class RoleTimeRequirement : JobRequirement
    {
        /// <summary>
        /// What particular role they need the time requirement with.
        /// </summary>
        [DataField("role")]
        public string Role = default!;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;
    }
}
