using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ReagentImplanter
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ReagentCapsuleComponent : Component
    {
        [DataField("isUsed")]
        public bool IsUsed = false;
    }

}
