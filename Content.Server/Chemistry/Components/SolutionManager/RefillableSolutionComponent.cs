using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Reagents that can be added easily. For example like
    ///     pouring something into another beaker, glass, or into the gas
    ///     tank of a car.
    /// </summary>
    [RegisterComponent]
    public class RefillableSolutionComponent : Component
    {
        public override string Name => "RefillableSolution";

        /// <summary>
        /// Solution name that can added to easily.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";

    }
}
