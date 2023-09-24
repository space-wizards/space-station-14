using Robust.Shared.GameStates;
using Content.Shared.Humanoid;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whistle
{
    /// <summary>
    /// Spawn attached entity for entities in range with <see cref="HumanoidAppearanceComponent"/>.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class WhistleComponent : Component
    {
        /// <summary>
        /// Entity prototype to spawn
        /// </summary>
        [DataField("effect", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Effect = "WhistleExclamation";

        /// <summary>
        /// Range value.
        /// </summary>
        [DataField("distance")]
        public float Distance = 0;
    }
}
