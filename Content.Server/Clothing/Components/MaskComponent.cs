using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Clothing.Components
{
    [Access(typeof(MaskSystem))]
    [RegisterComponent]
    public sealed partial class MaskComponent : Component
    {
        [DataField("toggleActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ToggleActionId = "ActionToggleMask";

        /// <summary>
        /// This mask can be toggled (pulled up/down)
        /// </summary>
        [DataField("toggleAction")]
        public EntityUid? ToggleAction;

        public bool IsToggled = false;
    }
}
