#nullable enable
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Disposal.DisposalUnit
{
    [Serializable, NetSerializable]
    public class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string UnitName;
        public readonly PressureState UnitState;
        public readonly bool Powered;
        public readonly bool Engaged;

        public DisposalUnitBoundUserInterfaceState(string unitName, PressureState unitState, bool powered, bool engaged)
        {
            UnitName = unitName;
            UnitState = unitState;
            Powered = powered;
            Engaged = engaged;
        }

        [Serializable, NetSerializable]
        public enum PressureState
        {
            Ready,
            Pressurizing
        }
    }
}
