using Content.Server.Body.Systems;
using Content.Shared.Body.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Body.Components
{
    /// <summary>
    ///     Handles metabolizing various reagents with given effects.
    /// </summary>
    [RegisterComponent, Access(typeof(MetabolizerSystem))]
    public sealed partial class MetabolizerComponent : Component
    {
        public float AccumulatedFrametime = 0.0f;

        /// <summary>
        ///     How often to metabolize reagents, in seconds.
        /// </summary>
        /// <returns></returns>
        [DataField]
        public float UpdateFrequency = 1.0f;

        /// <summary>
        ///     From which solution will this metabolizer attempt to metabolize chemicals
        /// </summary>
        [DataField("solution")]
        public string SolutionName { get; set; } = BloodstreamComponent.DefaultChemicalsSolutionName;

        /// <summary>
        ///     Does this component use a solution on it's parent entity (the body) or itself
        /// </summary>
        /// <remarks>
        ///     Most things will use the parent entity (bloodstream).
        /// </remarks>
        [DataField]
        public bool SolutionOnBody = true;

        /// <summary>
        ///     List of metabolizer types that this organ is. ex. Human, Slime, Felinid, w/e.
        /// </summary>
        [DataField(customTypeSerializer:typeof(PrototypeIdHashSetSerializer<MetabolizerTypePrototype>))]
        [Access(typeof(MetabolizerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public HashSet<string>? MetabolizerTypes = null;

        /// <summary>
        ///     Should this metabolizer remove chemicals that have no metabolisms defined?
        ///     As a stop-gap, basically.
        /// </summary>
        [DataField]
        public bool RemoveEmpty = false;

        /// <summary>
        ///     How many reagents can this metabolizer process at once?
        ///     Used to nerf 'stacked poisons' where having 5+ different poisons in a syringe, even at low
        ///     quantity, would be muuuuch better than just one poison acting.
        /// </summary>
        [DataField("maxReagents")]
        public int MaxReagentsProcessable = 3;

        /// <summary>
        ///     A list of metabolism groups that this metabolizer will act on, in order of precedence.
        /// </summary>
        [DataField("groups")]
        public List<MetabolismGroupEntry>? MetabolismGroups = default!;
    }

    /// <summary>
    ///     Contains data about how a metabolizer will metabolize a single group.
    ///     This allows metabolizers to remove certain groups much faster, or not at all.
    /// </summary>
    [DataDefinition]
    public sealed partial class MetabolismGroupEntry
    {
        [DataField(required: true, customTypeSerializer:typeof(PrototypeIdSerializer<MetabolismGroupPrototype>))]
        public string Id = default!;

        [DataField("rateModifier")]
        public FixedPoint2 MetabolismRateModifier = 1.0;
    }
}
