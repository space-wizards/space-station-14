using Content.Shared.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Body.Components
{
    /// <summary>
    ///     Handles metabolizing various reagents with given effects.
    /// </summary>
    [RegisterComponent, AutoGenerateComponentPause, Access(typeof(MetabolizerSystem))]
    public sealed partial class MetabolizerComponent : Component
    {
        /// <summary>
        ///     The next time that reagents will be metabolized.
        /// </summary>
        [DataField, AutoPausedField]
        public TimeSpan NextUpdate;

        /// <summary>
        ///     How often to metabolize reagents.
        /// </summary>
        /// <returns></returns>
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate multiplier.
        /// </summary>
        [DataField]
        public float UpdateIntervalMultiplier = 1f;

        /// <summary>
        /// Adjusted update interval based off of the multiplier value.
        /// </summary>
        [ViewVariables]
        public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

        /// <summary>
        ///     From which solution will this metabolizer attempt to metabolize chemicals
        /// </summary>
        [DataField("solution")]
        public string SolutionName = BloodstreamComponent.DefaultChemicalsSolutionName;

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
        [DataField]
        [Access(typeof(MetabolizerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public HashSet<ProtoId<MetabolizerTypePrototype>>? MetabolizerTypes;

        /// <summary>
        ///     Should this metabolizer remove chemicals that have no metabolisms defined?
        ///     As a stop-gap, basically.
        /// </summary>
        [DataField]
        public bool RemoveEmpty;

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
        public List<MetabolismGroupEntry>? MetabolismGroups;
    }

    /// <summary>
    ///     Contains data about how a metabolizer will metabolize a single group.
    ///     This allows metabolizers to remove certain groups much faster, or not at all.
    /// </summary>
    [DataDefinition]
    public sealed partial class MetabolismGroupEntry
    {
        [DataField(required: true)]
        public ProtoId<MetabolismGroupPrototype> Id;

        [DataField("rateModifier")]
        public FixedPoint2 MetabolismRateModifier = 1.0;
    }
}
