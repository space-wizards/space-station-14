using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class CleanableComponent : Component
    {
        public override string Name => "Cleanable";

        private ReagentUnit _cleanAmount;
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit CleanAmount => _cleanAmount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _cleanAmount, "cleanAmount", ReagentUnit.Zero);
        }
    }
}
