using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Kitchen
{
    /// <summary>
    /// Tag component that denotes an entity as Juiceable
    /// </summary>
    [RegisterComponent]
    public class JuiceableComponent : Component
    {
        public override string Name => "Juiceable";
        [ViewVariables] public Solution JuiceResultSolution;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.JuiceResultSolution, "result", new Solution());

        }
    }
}
