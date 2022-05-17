using System.Threading;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Server behavior for reagent injectors and syringes. Can optionally support both
    /// injection and drawing or just injection. Can inject/draw reagents from solution
    /// containers, and can directly inject into a mobs bloodstream.
    /// </summary>
    [RegisterComponent]
    public sealed class IVBagComponent : SharedIVBagComponent
    {
        public const string SolutionName = "ivbag";

        /// <summary>
        /// Injection delay (seconds) when the target is a mob.
        /// </summary>
        /// <remarks>
        /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
        /// in combat mode.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("injectDelay")]
        public float InjectDelay = 3.5f;

        /// <summary>
        /// The delay after injection before solution flow begins.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("startDelay")]
        public float StartDelay = 3f;

        /// <summary>
        /// The delay between flows after flowing has begun.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flowDelay")]
        public float FlowDelay = 1f;

        /// <summary>
        /// Amount of solution to flow into a bloodstream per interval.
        /// </summary>
        [ViewVariables]
        public FixedPoint2 FlowAmount = FixedPoint2.New(1);

        /// <summary>
        /// Amount of solution to transfer into containers.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount = FixedPoint2.New(10);


        /// <summary>
        /// Does the bag think it's injected into a mob?
        /// </summary>
        [ViewVariables]
        public bool Connected = false;

        /// <summary>
        /// What mob is the IV connected to?
        /// </summary>
        [ViewVariables]
        public EntityUid Mob;


        /// <summary>
        ///     Token for interrupting a do-after action (e.g., injection another player). If not null, implies
        ///     component is currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;

        private IVBagToggleMode _toggleState;


        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public IVBagToggleMode ToggleState
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
