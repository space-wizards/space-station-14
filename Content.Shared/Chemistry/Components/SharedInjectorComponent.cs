using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    [Serializable, NetSerializable]
    public sealed partial class InjectorDoAfterEvent : SimpleDoAfterEvent
    {
    }

    /// <summary>
    /// Shared class for injectors & syringes
    /// </summary>
    [NetworkedComponent, ComponentProtoName("Injector")]
    public abstract partial class SharedInjectorComponent : Component
    {
        /// <summary>
        /// Component data used for net updates. Used by client for item status ui
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class InjectorComponentState : ComponentState
        {
            public FixedPoint2 CurrentVolume { get; }
            public FixedPoint2 TotalVolume { get; }
            public SharedTransferToggleMode CurrentMode { get; }

            public InjectorComponentState(FixedPoint2 currentVolume, FixedPoint2 totalVolume,
                SharedTransferToggleMode currentMode)
            {
                CurrentVolume = currentVolume;
                TotalVolume = totalVolume;
                CurrentMode = currentMode;
            }
        }
    }
}
