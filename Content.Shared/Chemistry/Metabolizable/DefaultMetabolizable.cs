using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Metabolizable
{
    /// <summary>
    ///     Default metabolization for reagents. Returns the amount of reagents metabolized without applying effects.
    ///     Metabolizes reagents at a constant rate, limited by how much is available. Other classes are derived from
    ///     this class, so that they do not need their own metabolization quantity calculation.
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

            // How much reagent should we metabolize
            // The default behaviour is to metabolize at a constant rate, independent of the quantity of reagents.
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
