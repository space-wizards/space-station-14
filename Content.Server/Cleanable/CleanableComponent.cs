using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cleanable
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
