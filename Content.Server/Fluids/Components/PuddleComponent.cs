using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Fluids.Components
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(PuddleSystem))]
    public sealed class PuddleComponent : Component
    {
        public const string DefaultSolutionName = "puddle";
        private static readonly ReagentUnit DefaultSlipThreshold = ReagentUnit.New(3);
        public static readonly ReagentUnit DefaultOverflowVolume = ReagentUnit.New(20);

        public override string Name => "Puddle";


        // Current design: Something calls the SpillHelper.Spill, that will either
        // A) Add to an existing puddle at the location (normalised to tile-center) or
        // B) add a new one
        // From this every time a puddle is spilt on it will try and overflow to its neighbours if possible,
        // and also update its appearance based on volume level (opacity) and chemistry color
        // Small puddles will evaporate after a set delay

        // TODO: 'leaves fluidtracks', probably in a separate component for stuff like gibb chunks?;

        // based on behaviour (e.g. someone being punched vs slashed with a sword would have different blood sprite)
        // to check for low volumes for evaporation or whatever


        [DataField("slipThreshold")] public ReagentUnit SlipThreshold = DefaultSlipThreshold;

        [DataField("spillSound")]
        public SoundSpecifier SpillSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        /// <summary>
        /// Whether or not this puddle is currently overflowing onto its neighbors
        /// </summary>
        public bool Overflown;

        [ViewVariables(VVAccess.ReadOnly)]
        public ReagentUnit CurrentVolume => EntitySystem.Get<PuddleSystem>().CurrentVolume(Owner.Uid);

        [ViewVariables] [DataField("overflowVolume")]
        public ReagentUnit OverflowVolume = DefaultOverflowVolume;

        public ReagentUnit OverflowLeft => CurrentVolume - OverflowVolume;

        [DataField("solution")] public string SolutionName { get; set; } = DefaultSolutionName;
    }
}
