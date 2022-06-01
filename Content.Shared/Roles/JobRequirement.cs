namespace Content.Shared.Roles
{
    [DataDefinition]
    public sealed class JobRequirement
    {
        [DataField("job")]
        public string? Job;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public int Time;
    }
}
