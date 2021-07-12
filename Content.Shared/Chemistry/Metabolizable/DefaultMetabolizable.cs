#nullable enable
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Metabolizable
{
    /// <summary>
    ///     Default metabolism for reagents. Metabolizes the reagent with no effects
    /// </summary>
    [DataDefinition]
    public class DefaultMetabolizable : IMetabolizable
    {
        /// <summary>
        ///     Rate of metabolism in units / second
        /// </summary>
        [DataField("rate")]
        public double MetabolismRate { get; set; } = 1;

        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            return ReagentUnit.New(MetabolismRate * tickTime);
        }
    }
}
