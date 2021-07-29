using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Metabolism
{
    /// <summary>
    ///     Handles metabolizing various reagents with given effects.
    /// </summary>
    public class MetabolizerComponent : Component
    {
        public override string Name => "Metabolizer";

        public float AccumulatedFrametime = 0.0f;

        /// <summary>
        ///     How often to metabolize reagents, in seconds.
        /// </summary>
        /// <returns></returns>
        [DataField("updateFrequency")]
        public float UpdateFrequency = 1.0f;

        /// <summary>
        ///     A list of metabolisms
        /// </summary>
        /// <returns></returns>
        [DataField("metabolism", required: true)]
        public List<ReagentEffectsEntry> Metabolisms = default!;
    }

    [DataDefinition]
    public class ReagentEffectsEntry
    {
        /// <summary>
        ///     List of reagents associated with this metabolism rate and effects.
        /// </summary>
        [DataField("reagents", required: true)]
        public List<string> Reagents = default!;

        /// <summary>
        ///     Amount of reagent to metabolize, per metabolism cycle.
        /// </summary>
        [DataField("metabolismRate")]
        public float MetabolismRate = 1.0f;

        /// <summary>
        ///     A list of effects to apply when these reagents are metabolized.
        /// </summary>
        [DataField("effects", required: true)]
        public List<ReagentEffect> Effects = default!;
    }
}
