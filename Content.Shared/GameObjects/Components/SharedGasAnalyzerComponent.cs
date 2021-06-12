using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedGasAnalyzerComponent : Component
    {
        public override string Name => "GasAnalyzer";
        public override uint? NetID => ContentNetIDs.GAS_ANALYZER;

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
            public GasEntry[]? Gases;
            public string? Error;

            public GasAnalyzerBoundUserInterfaceState(float pressure, float temperature, GasEntry[]? gases, string? error = null)
            {
                Pressure = pressure;
                Temperature = temperature;
                Gases = gases;
                Error = error;
            }
        }

        [Serializable, NetSerializable]
        public struct GasEntry
        {
            public readonly string Name;
            public readonly float Amount;
            public readonly string Color;

            public GasEntry(string name, float amount, string color)
            {
                Name = name;
                Amount = amount;
                Color = color;
            }

            public override string ToString()
            {
                // e.g. "Plasma: 2000 mol"
                return Loc.GetString(
                    "gas-entry-info",
                     ("gasName", Name),
                     ("gasAmount", Amount));
            }
        }

        [Serializable, NetSerializable]
        public class GasAnalyzerRefreshMessage : BoundUserInterfaceMessage
        {
            public GasAnalyzerRefreshMessage() {}
        }

        [Serializable, NetSerializable]
        public enum GasAnalyzerDanger
        {
            Nominal,
            Warning,
            Hazard
        }

        [Serializable, NetSerializable]
        public class GasAnalyzerComponentState : ComponentState
        {
            public GasAnalyzerDanger Danger;

            public GasAnalyzerComponentState(GasAnalyzerDanger danger) : base(ContentNetIDs.GAS_ANALYZER)
            {
                Danger = danger;
            }
        }
    }
}
