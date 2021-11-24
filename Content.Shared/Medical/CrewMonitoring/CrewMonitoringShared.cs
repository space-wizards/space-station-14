using System;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring
{
    [Serializable, NetSerializable]
    public enum CrewMonitoringUIKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public class CrewMonitoringState : BoundUserInterfaceState
    {

    }
}
