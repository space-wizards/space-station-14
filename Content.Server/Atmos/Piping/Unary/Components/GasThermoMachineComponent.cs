using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasThermoMachineComponent : Component, ISerializationHooks
    {
        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity { get; set; } = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public float TargetTemperature { get; set; } = Atmospherics.T20C;

        [DataField("mode")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ThermoMachineMode Mode { get; set; } = ThermoMachineMode.Freezer;

        [DataField("minTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinTemperature { get; set; } = Atmospherics.T20C;

        [DataField("maxTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxTemperature { get; set; } = Atmospherics.T20C;

        public float InitialMinTemperature { get; private set; }
        public float InitialMaxTemperature { get; private set; }

        void ISerializationHooks.AfterDeserialization()
        {
            InitialMinTemperature = MinTemperature;
            InitialMaxTemperature = MaxTemperature;
        }
    }

    public enum ThermoMachineMode : byte
    {
        Freezer = 0,
        Heater = 1,
    }
}
