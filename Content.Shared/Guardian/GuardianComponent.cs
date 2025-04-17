using Robust.Shared.GameStates;

namespace Content.Shared.Guardian
{
    /// <summary>
    /// Given to guardians to monitor their link with the host
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class GuardianComponent : Component
    {
        /// <summary>
        /// The guardian host entity
        /// </summary>
        [DataField, AutoNetworkedField]
        public EntityUid? Host;

        /// <summary>
        /// Percentage of damage reflected from the guardian to the host
        /// </summary>
        [DataField, AutoNetworkedField]
        public float DamageShare { get; set; } = 0.65f;

        /// <summary>
        /// Maximum distance the guardian can travel before it's forced to recall, use YAML to set
        /// </summary>
        [DataField, AutoNetworkedField]
        public float DistanceAllowed { get; set; } = 5f;

        /// <summary>
        /// If the guardian is currently manifested
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool GuardianLoose;

    }
}
