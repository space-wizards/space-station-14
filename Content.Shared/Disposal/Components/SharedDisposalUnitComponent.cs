using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components
{
    [NetworkedComponent]
    public abstract class SharedDisposalUnitComponent : Component
    {
        public const string ContainerId = "DisposalUnit";

        // TODO: Could maybe turn the contact off instead far more cheaply as farseer (though not box2d) had support for it?
        // Need to suss it out.
        /// <summary>
        /// We'll track whatever just left disposals so we know what collision we need to ignore until they stop intersecting our BB.
        /// </summary>
        public List<EntityUid> RecentlyEjected = new();

        [DataField("flushTime", required: true)]
        public readonly float FlushTime;

        [DataField("mobsCanEnter")]
        public bool MobsCanEnter = true;

        [Serializable, NetSerializable]
        public enum Visuals : byte
        {
            VisualState,
            Handle,
            Light
        }

        [Serializable, NetSerializable]
        public enum VisualState : byte
        {
            UnAnchored,
            Anchored,
            Flushing,
            Charging
        }

        [Serializable, NetSerializable]
        public enum HandleState : byte
        {
            Normal,
            Engaged
        }

        [Serializable, NetSerializable]
        public enum LightStates : byte
        {
            Off = 0,
            Charging = 1 << 0,
            Full = 1 << 1,
            Ready = 1 << 2
        }

        [Serializable, NetSerializable]
        public enum UiButton : byte
        {
            Eject,
            Engage,
            Power
        }

        [Serializable, NetSerializable]
        public enum PressureState : byte
        {
            Ready,
            Pressurizing
        }

        public override ComponentState GetComponentState()
        {
            return new DisposalUnitComponentState(RecentlyEjected);
        }

        [Serializable, NetSerializable]
        protected sealed class DisposalUnitComponentState : ComponentState
        {
            public List<EntityUid> RecentlyEjected;

            public DisposalUnitComponentState(List<EntityUid> uids)
            {
                RecentlyEjected = uids;
            }
        }

        [Serializable, NetSerializable]
        public sealed class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState, IEquatable<DisposalUnitBoundUserInterfaceState>
        {
            public readonly string UnitName;
            public readonly string UnitState;
            public readonly TimeSpan FullPressureTime;
            public readonly bool Powered;
            public readonly bool Engaged;

            public DisposalUnitBoundUserInterfaceState(string unitName, string unitState, TimeSpan fullPressureTime, bool powered,
                bool engaged)
            {
                UnitName = unitName;
                UnitState = unitState;
                FullPressureTime = fullPressureTime;
                Powered = powered;
                Engaged = engaged;
            }

            public bool Equals(DisposalUnitBoundUserInterfaceState? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return UnitName == other.UnitName &&
                       UnitState == other.UnitState &&
                       Powered == other.Powered &&
                       Engaged == other.Engaged &&
                       FullPressureTime.Equals(other.FullPressureTime);
            }
        }

        /// <summary>
        ///     Message data sent from client to server when a disposal unit ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

        [Serializable, NetSerializable]
        public enum DisposalUnitUiKey : byte
        {
            Key
        }
    }
}
