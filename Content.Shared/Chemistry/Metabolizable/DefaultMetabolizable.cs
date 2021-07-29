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
        [DataField("rate")] public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

        public virtual ReagentUnit Metabolize(IEntity solutionEntity, string reagentId, float tickTime, ReagentUnit availableReagent)
        {

            // how much reagent should we metabolize
            var amountMetabolized = MetabolismRate * tickTime;

            // is that much reagent actually available?
            if (availableReagent < amountMetabolized)
            {
                return availableReagent;
            }

            return amountMetabolized;
        }
    }
}
