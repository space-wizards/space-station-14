using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Rotatable
{
    [RegisterComponent]
    public sealed partial class FlippableComponent : Component
    {
        /// <summary>
        ///     Entity to replace this entity with when the current one is 'flipped'.
        /// </summary>
        [DataField("mirrorEntity", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string MirrorEntity = default!;
    }
}
