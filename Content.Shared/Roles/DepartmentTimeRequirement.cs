using JetBrains.Annotations;

namespace Content.Shared.Roles
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
    }
}
