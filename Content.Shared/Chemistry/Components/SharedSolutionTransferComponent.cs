using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    [NetworkedComponent]
    public abstract partial class SharedSolutionTransferComponent : Component
    {
        /// <summary>
        /// Component data used for net updates. Used by client for item status ui
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class SolutionTransferComponentState : ComponentState
        {
            public FixedPoint2 CurrentVolume { get; }
            public FixedPoint2 TotalVolume { get; }
            public SharedTransferToggleMode? CurrentMode { get; } = SharedTransferToggleMode.Draw;

            public SolutionTransferComponentState(FixedPoint2 currentVolume, FixedPoint2 totalVolume,
                SharedTransferToggleMode? currentMode)
            {
                CurrentVolume = currentVolume;
                TotalVolume = totalVolume;
                CurrentMode = currentMode;
            }
        }
    }
}
