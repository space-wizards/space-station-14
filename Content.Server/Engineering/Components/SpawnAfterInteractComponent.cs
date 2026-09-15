using Robust.Shared.Prototypes;

namespace Content.Server.Engineering.Components
{
    /// <summary>
    /// This component enables the spawn of a specific entity upon being interacted with.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnAfterInteractComponent : Component
    {
        /// <summary>
        /// The prototype id of the entity to be spawned in.
        /// </summary>
        [DataField(required: true)]
        public EntProtoId Prototype;

        /// <summary>
        /// Stops the spawn if the target space is no longer in reach, unless true.
        /// </summary>
        [DataField]
        public bool IgnoreDistance;

        /// <summary>
        /// The length of the interact action.
        /// </summary>
        [DataField("doAfter")]
        public float DoAfterTime = 0;

        /// <summary>
        /// If the entity with this component should be deleted upon a successful spawn.
        /// </summary>
        [DataField]
        public bool RemoveOnInteract;
    }
}
