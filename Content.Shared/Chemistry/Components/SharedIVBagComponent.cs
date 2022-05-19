using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Shared class for injectors & syringes
    /// </summary>
    [NetworkedComponent, ComponentProtoName("IVBag")]
    public abstract class SharedIVBagComponent : Component
    {
        /// <summary>
        /// Component data used for net updates. Used by client for item status ui
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class IVBagComponentState : ComponentState
        {
            public FixedPoint2 CurrentVolume { get; }
            public FixedPoint2 TotalVolume { get; }
            public IVBagToggleMode CurrentMode { get; }
            public bool Connected { get; }

            public IVBagComponentState(FixedPoint2 currentVolume, FixedPoint2 totalVolume, SharedIVBagComponent.IVBagToggleMode currentMode, bool bConnected)
            {
                CurrentVolume = currentVolume;
                TotalVolume = totalVolume;
                CurrentMode = currentMode;
                Connected = bConnected;
            }
        }

        public enum IVBagToggleMode : byte
        {
            Inject,
            Draw,
            Closed
        }
    }
}
