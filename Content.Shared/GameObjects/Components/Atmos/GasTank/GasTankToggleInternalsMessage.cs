#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos.GasTank
{
    [Serializable, NetSerializable]
    public class GasTankToggleInternalsMessage : BoundUserInterfaceMessage
    {
    }
}
