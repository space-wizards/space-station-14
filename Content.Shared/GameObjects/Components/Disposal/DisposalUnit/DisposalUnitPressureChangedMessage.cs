using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Disposal.DisposalUnit
{
    [Serializable, NetSerializable]
    public class DisposalUnitPressureChangedMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; }
        public float TargetPressure { get; }

        public DisposalUnitPressureChangedMessage(float pressure, float targetPressure)
        {
            Pressure = pressure;
            TargetPressure = targetPressure;
        }
    }
}
