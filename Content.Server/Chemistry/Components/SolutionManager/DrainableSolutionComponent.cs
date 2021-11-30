using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Denotes the solution that can be easily removed through any reagent container.
    ///     Think pouring this or draining from a water tank.
    /// </summary>
    [RegisterComponent]
    public class DrainableSolutionComponent : Component
    {
        public override string Name => "DrainableSolution";

        /// <summary>
        /// Solution name that can be drained.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
