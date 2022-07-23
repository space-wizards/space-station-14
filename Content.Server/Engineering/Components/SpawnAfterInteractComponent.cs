using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Engineering.Components
{
    [RegisterComponent]
    public sealed class SpawnAfterInteractComponent : Component
    {
        [ViewVariables]
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; }

        [ViewVariables]
        [DataField("ignoreDistance")]
        public bool IgnoreDistance { get; }

        [ViewVariables]
        [DataField("doAfter")]
        public float DoAfterTime = 0;

        [ViewVariables]
        [DataField("removeOnInteract")]
        public bool RemoveOnInteract = false;
    }
}
