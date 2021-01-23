#nullable enable
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Disposal.DisposalUnit
{
    [Serializable, NetSerializable]
    public class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState, IEquatable<DisposalUnitBoundUserInterfaceState>
    {
        public readonly string UnitName;
        public readonly string UnitState;
        public readonly float Pressure;
        public readonly bool Powered;
        public readonly bool Engaged;

        public DisposalUnitBoundUserInterfaceState(string unitName, string unitState, float pressure, bool powered, bool engaged)
        {
            UnitName = unitName;
            UnitState = unitState;
            Pressure = pressure;
            Powered = powered;
            Engaged = engaged;
        }

        public bool Equals(DisposalUnitBoundUserInterfaceState? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return UnitName == other.UnitName &&
                   UnitState == other.UnitState &&
                   Powered == other.Powered &&
                   Engaged == other.Engaged &&
                   Pressure.Equals(other.Pressure);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DisposalUnitBoundUserInterfaceState);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
