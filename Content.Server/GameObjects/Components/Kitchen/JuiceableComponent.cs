using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Kitchen
{
    /// <summary>
    /// Tag component that denotes an entity as Juiceable
    /// </summary>
    [RegisterComponent]
    public class JuiceableComponent : Component
    {
        public override string Name => "Juiceable";

        public Solution JuiceResultSolution => _solution;

        private Solution _solution;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => _solution, "result", new Solution());

        }

    }
}
