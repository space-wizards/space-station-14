using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components
{
    [NetworkedComponent()]
    public abstract class SharedGasAnalyzerComponent : Component
    {
        [Serializable, NetSerializable]
        public enum GasAnalyzerUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public sealed class GasAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
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
        public sealed class GasAnalyzerRefreshMessage : BoundUserInterfaceMessage
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
        public sealed class GasAnalyzerComponentState : ComponentState
        {
            public GasAnalyzerDanger Danger;

            public GasAnalyzerComponentState(GasAnalyzerDanger danger)
            {
                Danger = danger;
            }
        }
    }

    [NetSerializable]
    [Serializable]
    public enum GasAnalyzerVisuals
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum GasAnalyzerVisualState
    {
        Off,
        Working,
    }
}
