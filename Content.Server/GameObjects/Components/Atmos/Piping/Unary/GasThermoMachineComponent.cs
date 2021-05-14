using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.Construction;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Unary
{
    [RegisterComponent]
    public class GasThermoMachineComponent : Component, IAtmosProcess, IRefreshParts, ISerializationHooks
    {
        public override string Name => "GasThermoMachine";

        [DataField("inlet")]
        private string _inletName = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _heatCapacity = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _targetTemperature = Atmospherics.T20C;

        [DataField("mode")]
        [ViewVariables(VVAccess.ReadWrite)]
        private ThermoMachineMode _mode = ThermoMachineMode.Freezer;

        [DataField("minTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        private float _minTemperature = Atmospherics.T20C;

        [DataField("maxTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        private float _maxTemperature = Atmospherics.T20C;

        private float _initialMinTemperature;
        private float _initialMaxTemperature;

        public void ProcessAtmos(IGridAtmosphereComponent atmosphere)
        {
            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? inlet))
                return;

            var airHeatCapacity = inlet.Air.HeatCapacity;
            var combinedHeatCapacity = airHeatCapacity + _heatCapacity;
            var oldTemperature = inlet.Air.Temperature;

            if (combinedHeatCapacity > 0)
            {
                var combinedEnergy = _heatCapacity * _targetTemperature + airHeatCapacity * inlet.Air.Temperature;
                inlet.Air.Temperature = combinedEnergy / combinedHeatCapacity;
            }

            // TODO ATMOS: Active power usage.
        }

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

            _heatCapacity = 5000 * MathF.Pow((matterBinRating - 1), 2);

            switch (_mode)
            {
                // 573.15K with stock parts.
                case ThermoMachineMode.Heater:
                    _maxTemperature = Atmospherics.T20C + (_initialMaxTemperature * laserRating);
                    break;
                // 73.15K with stock parts.
                case ThermoMachineMode.Freezer:
                    _minTemperature = MathF.Max(Atmospherics.T0C - _initialMinTemperature + laserRating * 15f, Atmospherics.TCMB);
                    break;
            }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            _initialMinTemperature = _minTemperature;
            _initialMaxTemperature = _maxTemperature;
        }
    }

    public enum ThermoMachineMode : byte
    {
        Freezer = 0,
        Heater = 1,
    }
}
