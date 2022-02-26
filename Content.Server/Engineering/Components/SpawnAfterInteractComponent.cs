using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Engineering.Components
{
    [RegisterComponent]
    public sealed class SpawnAfterInteractComponent : Component
    {
        [ViewVariables]
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; }

        [ViewVariables]
        [DataField("doAfter")]
        public float DoAfterTime = 0;

        [ViewVariables]
        [DataField("removeOnInteract")]
        public bool RemoveOnInteract = false;
    }
}
