using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Server behavior for reagent injectors and syringes. Can optionally support both
    /// injection and drawing or just injection. Can inject/draw reagents from solution
    /// containers, and can directly inject into a mobs bloodstream.
    /// </summary>
    [RegisterComponent]
    public sealed partial class InjectorComponent : SharedInjectorComponent
    {
        public const string SolutionName = "injector";

        /// <summary>
        /// Whether or not the injector is able to draw from containers or if it's a single use
        /// device that can only inject.
        /// </summary>
        [DataField("injectOnly")]
        public bool InjectOnly;

        /// <summary>
        /// Whether or not the injector is able to draw from or inject from mobs
        /// </summary>
        /// <remarks>
        ///     for example: droppers would ignore mobs
        /// </remarks>
        [DataField("ignoreMobs")]
        public bool IgnoreMobs = false;

        /// <summary>
        ///     The minimum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("minTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MinimumTransferAmount { get; set; } = FixedPoint2.New(5);

        /// <summary>
        ///     The maximum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("maxTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MaximumTransferAmount { get; set; } = FixedPoint2.New(50);

        /// <summary>
        /// Amount to inject or draw on each usage. If the injector is inject only, it will
        /// attempt to inject it's entire contents upon use.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public FixedPoint2 TransferAmount = FixedPoint2.New(5);

        /// <summary>
        /// Injection delay (seconds) when the target is a mob.
        /// </summary>
        /// <remarks>
        /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
        /// in combat mode.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 5;

        [DataField("toggleState")] private InjectorToggleMode _toggleState;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
        /// only ever be set to Inject
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public InjectorToggleMode ToggleState
        {
            get => _toggleState;
            set
            {
                _toggleState = value;
                Dirty();
            }
        }
    }
}
