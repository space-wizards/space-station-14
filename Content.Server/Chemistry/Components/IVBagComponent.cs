using System.Threading;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Server behavior for IV bags. They inject and draw over time and
    /// rip out if pulled too far or are put in containers. This is
    /// accomplished with IVTargetComponent and IVHolderComponent.
    /// </summary>
    [RegisterComponent]
    public sealed class IVBagComponent : SharedIVBagComponent
    {
        public const string SolutionName = "ivbag";

        public CancellationTokenSource? FlowCancel;
        public CancellationTokenSource? InjectCancel;
        public Boolean StartingUp;


        /// <summary> Does the bag assume it's injected into a mob? </summary>
        [ViewVariables]
        public bool Connected = false;

        /// <summary> What mob is the IV connected to? </summary>
        [ViewVariables]
        public EntityUid? Target;
        public TransformComponent? BagPos;
        public TransformComponent? TargetPos;


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

        /// <summary> The delay after injection before solution flow begins. </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flowStartDelay")]
        public TimeSpan FlowStartDelay = TimeSpan.FromSeconds(2.0f);


        /// <summary> A list of delays the player can choose from. </summary>
        [DataField("flowDelayOptions")]
        public static int[] FlowDelayOptions = { 1, 2, 3, 4, 6, 8 };

        /// <summary> The delay between flows after flowing has begun. </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan FlowDelay = TimeSpan.FromSeconds(FlowDelayOptions[0]);

        /// <summary> Amount of solution to flow into a bloodstream per interval. </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 FlowAmount = FixedPoint2.New(2.0f);

        /// <summary> The limit of chems per drip when connected to a mob. </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 FlowChem = FixedPoint2.New(0.2f); // Mostly blood (for balance).

        /// <summary> Amount of solution to transfer into containers. </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 PourAmount = FixedPoint2.New(10f);


        private IVBagToggleMode _toggleState;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public IVBagToggleMode FlowState
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
