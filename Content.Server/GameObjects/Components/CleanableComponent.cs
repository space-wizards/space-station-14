using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class CleanableComponent : Component
    {
        public override string Name => "Cleanable";

        [DataField("cleanAmount")]
        private ReagentUnit _cleanAmount = ReagentUnit.Zero;
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit CleanAmount => _cleanAmount;
    }
}
