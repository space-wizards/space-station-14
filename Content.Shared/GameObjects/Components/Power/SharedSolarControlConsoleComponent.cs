using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    public class SharedSolarControlConsoleComponent : Component
    {
        public override string Name => "SolarControlConsole";

    }

    [Serializable, NetSerializable]
    public class SolarControlConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public SolarControlConsoleBoundInterfaceState()
        {
        }
    }

    [Serializable, NetSerializable]
    public enum SolarControlConsoleUiKey
    {
        Key
    }
}
