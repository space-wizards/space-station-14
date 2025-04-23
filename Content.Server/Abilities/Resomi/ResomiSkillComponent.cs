using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities.Resomi
{
    [RegisterComponent]
    public sealed partial class ResomiSkillComponent : Component
    {
        [DataField("actionJumpId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionJumpId = "Jump";
        
        [DataField]
        public float MaxThrow = 10f;
    }
}
