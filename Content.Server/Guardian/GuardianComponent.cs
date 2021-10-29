using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Computer;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Guardian
{
    /// <summary>
    /// Given to guardians to monitor their link with the host
    /// </summary>
    [RegisterComponent]
    public class GuardianComponent : Component
    {
        public override string Name => "Guardian";

        /// <summary>
        /// The guardian entity
        /// </summary>
        public EntityUid Guardian;

        /// <summary>
        /// The guardian host entity
        /// </summary>
        public EntityUid Host;

        /// <summary>
        /// Percentage of damage reflected from the guardian to the host, use f
        /// </summary>
        [ViewVariables] [DataField("damageShare")] public float DamagePercent { get; set; } = default!;

        /// <summary>
        /// Maximum distance the guardian can travel before it's forced to recall, use YAML to set
        /// </summary>
        [ViewVariables] [DataField("distanceAllowed")] public float DistanceAllowed { get; set; } = default!;

        /// <summary>
        /// If the guardian is currently manifested
        /// </summary>
        public bool Guardianloose = false;

    }
}
