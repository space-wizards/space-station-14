using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Localization;
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
            public float Pressure;
            public float Temperature;
            public StatusEntry[] Gases;
            public string Error;

            public GasAnalyzerBoundUserInterfaceState(float pressure, float temperature, StatusEntry[] gases, string error = null)
            {
                Pressure = pressure;
                Temperature = temperature;
                Gases = gases;
                Error = error;
            }
        }

        [Serializable, NetSerializable]
        public struct StatusEntry
        {
            public readonly object Key;
            public readonly object Value;

            public StatusEntry(object key, object value)
            {
                Key = key;
                Value = value;
            }

            public override string ToString()
            {
                return Loc.GetString("{0}: {1} mol", Key, Value);
            }
        }

        [Serializable, NetSerializable]
        public class GasAnalyzerRefreshMessage : BoundUserInterfaceMessage
        {
            public GasAnalyzerRefreshMessage() {}
        }
    }
}
