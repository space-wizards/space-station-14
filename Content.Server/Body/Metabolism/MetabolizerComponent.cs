using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Content.Shared.Body.Networks;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Body.Metabolism
{
    /// <summary>
    ///     Handles metabolizing various reagents with given effects.
    /// </summary>
    [RegisterComponent, Friend(typeof(MetabolizerSystem))]
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
        ///     From which solution will this metabolizer attempt to metabolize chemicals
        /// </summary>
        [DataField("solution")]
        public string SolutionName { get; set; } = SharedBloodstreamComponent.DefaultSolutionName;

        /// <summary>
        ///     Does this component use a solution on it's parent entity (the body) or itself
        /// </summary>
        /// <remarks>
        ///     Most things will use the parent entity (bloodstream). Something like the stomach will probably use
        ///     its own entity.
        /// </remarks>
        public bool SolutionOnBody = true;

        /// <summary>
        ///     List of metabolizer types that this organ is. ex. Human, Slime, Felinid, w/e.
        /// </summary>
        [DataField("types", required: true)]
        public List<string> MetabolizerTypes = default!;

        /// <summary>
        ///     A list of metabolism groups that this metabolizer will act on, in order of precedence.
        /// </summary>
        [DataField("groups", required: true)]
        public List<MetabolismGroupEntry> MetabolismGroups = default!;
    }

    /// <summary>
    ///     Contains data about how a metabolizer will metabolize a single group.
    ///     This allows metabolizers to remove certain groups much faster, or not at all.
    /// </summary>
    [DataDefinition]
    public class MetabolismGroupEntry
    {
        [DataField("id", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<MetabolismGroupPrototype>))]
        public string Id = default!;

        [DataField("rateModifier")]
        public FixedPoint2 MetabolismRateModifier = 1.0;
    }
}
