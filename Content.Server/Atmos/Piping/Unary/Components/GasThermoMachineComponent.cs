using System;
using System.Collections.Generic;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public class GasThermoMachineComponent : Component, IRefreshParts, ISerializationHooks
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

        void IRefreshParts.RefreshParts(IEnumerable<MachinePartComponent> parts)
        {
            var matterBinRating = 0;
            var laserRating = 0;

            foreach (var part in parts)
            {
                switch (part.PartType)
                {
                    case MachinePart.MatterBin:
                        matterBinRating += part.Rating;
                        break;
                    case MachinePart.Laser:
                        laserRating += part.Rating;
                        break;
                }
            }

            HeatCapacity = 5000 * MathF.Pow((matterBinRating - 1), 2);

            switch (Mode)
            {
                // 573.15K with stock parts.
                case ThermoMachineMode.Heater:
                    MaxTemperature = Atmospherics.T20C + (InitialMaxTemperature * laserRating);
                    break;
                // 73.15K with stock parts.
                case ThermoMachineMode.Freezer:
                    MinTemperature = MathF.Max(Atmospherics.T0C - InitialMinTemperature + laserRating * 15f, Atmospherics.TCMB);
                    break;
            }
        }

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
