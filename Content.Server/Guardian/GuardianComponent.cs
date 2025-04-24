namespace Content.Server.Guardian
{
    /// <summary>
    /// Given to guardians to monitor their link with the host
    /// </summary>
    [RegisterComponent]
    public sealed partial class GuardianComponent : Component
    {
        /// <summary>
        /// The guardian host entity
        /// </summary>
        [DataField]
        public EntityUid? Host;

        /// <summary>
        /// Percentage of damage reflected from the guardian to the host
        /// </summary>
        [DataField]
        public float DamageShare { get; set; } = 0.65f;

        /// <summary>
        /// Maximum distance the guardian can travel before it's forced to recall, use YAML to set
        /// </summary>
        [DataField]
        public float DistanceAllowed { get; set; } = 5f;

        /// <summary>
        /// If the guardian is currently manifested
        /// </summary>
        [DataField]
        public bool GuardianLoose;

    }
}
