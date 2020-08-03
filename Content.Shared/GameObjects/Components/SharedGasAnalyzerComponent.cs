using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.GameObjects.Components
{
    public class SharedGasAnalyzerComponent : Component
    {
        public override string Name => "GasAnalyzer";

        [Serializable, NetSerializable]
        public enum GasAnalyzerUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public class GasAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
        {
            //float Temperature
            //float Pressure
            //[]Gases
            /*public string BoardName { get; }
            public string SerialNumber { get; }
            public ClientWire[] WiresList { get; }
            public StatusEntry[] Statuses { get; }
            public int WireSeed { get; }

            public WiresBoundUserInterfaceState(ClientWire[] wiresList, StatusEntry[] statuses, string boardName, string serialNumber, int wireSeed)
            {
                BoardName = boardName;
                SerialNumber = serialNumber;
                WireSeed = wireSeed;
                WiresList = wiresList;
                Statuses = statuses;
            }*/
        }
    }
}
