using System;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos.GasTank
{
    [Serializable, NetSerializable]
    public class GasTankSetPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; set; }
    }
}
