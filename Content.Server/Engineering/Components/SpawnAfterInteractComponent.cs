using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Engineering.Components
{
    [RegisterComponent]
    public sealed class SpawnAfterInteractComponent : Component
    {
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; }

        [DataField("ignoreDistance")]
        public bool IgnoreDistance { get; }

        [DataField("doAfter")]
        public float DoAfterTime = 0;

        [DataField("removeOnInteract")]
        public bool RemoveOnInteract = false;
    }
}
