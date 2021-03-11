using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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
        [ViewVariables] [DataField("result")] public Solution JuiceResultSolution = new();
    }
}
