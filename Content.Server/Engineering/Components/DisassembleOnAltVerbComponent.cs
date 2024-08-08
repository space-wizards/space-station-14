using Robust.Shared.Prototypes;

namespace Content.Server.Engineering.Components
{
    [RegisterComponent]
    public sealed partial class DisassembleOnAltVerbComponent : Component
    {
        [DataField]
        public EntProtoId? Prototype { get; private set; }

        [DataField("doAfter")]
        public float DoAfterTime = 0;
    }
}
