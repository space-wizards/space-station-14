using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Rotatable
{
    [RegisterComponent]
    public class FlippableComponent : Component
    {
        public override string Name => "Flippable";

        /// <summary>
        ///     Entity to replace this entity with when the current one is 'flipped'.
        /// </summary>
        [DataField("mirrorEntity", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string MirrorEntity = default!;
    }
}
