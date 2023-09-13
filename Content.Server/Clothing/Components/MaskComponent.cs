using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Clothing.Components
{
    [Access(typeof(MaskSystem))]
    [RegisterComponent]
    public sealed partial class MaskComponent : Component
    {
        [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ToggleAction = "ActionToggleMask";

        /// <summary>
        /// This mask can be toggled (pulled up/down)
        /// </summary>
        [DataField("toggleActionEntity")]
        public EntityUid? ToggleActionEntity;

        public bool IsToggled = false;
    }
}
