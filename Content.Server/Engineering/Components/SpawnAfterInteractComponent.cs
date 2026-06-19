using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Engineering.Components
{
    /// <summary>
    /// This component enables the spawn of a specific entity upon being interacted with.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnAfterInteractComponent : Component
    {
        /// <summary>
        /// The prototype id of the entity to be spawned in
        /// </summary>
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; private set; }

        /// <summary>
        /// If true will not work if not in reach.
        /// </summary>
        [DataField("ignoreDistance")]
        public bool IgnoreDistance { get; private set; }

        /// <summary>
        /// The length of the interact action
        /// </summary>
        [DataField("doAfter")]
        public float DoAfterTime = 0;

        /// <summary>
        /// if the entity with this component should be deleted upon successful spawn.
        /// </summary>
        [DataField("removeOnInteract")]
        public bool RemoveOnInteract = false;
    }
}
