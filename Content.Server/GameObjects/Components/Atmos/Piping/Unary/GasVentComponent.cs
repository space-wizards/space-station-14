using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Unary
{
    [RegisterComponent]
    public class GasVentComponent : Component, IAtmosProcess
    {
        public override string Name => "GasVent";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        private string _inletName = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPressureBound PressureChecks { get; set; } = VentPressureBound.ExternalBound;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ExternalPressureBound = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        public float InternalPressureBound = 0f;

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            // TODO ATMOS: Weld shut.
            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? pipe))
                return;

            var environment = atmosphere.GetTile(Owner.Transform.Coordinates)!;

            // We're in an air-blocked tile... Do nothing.
            if (environment.Air == null)
                return;

            if (PumpDirection == VentPumpDirection.Releasing)
            {
                var pressureDelta = 10000f;

                if ((PressureChecks & VentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, ExternalPressureBound - environment.Air.Pressure);

                if ((PressureChecks & VentPressureBound.InternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, pipe.Air.Pressure - InternalPressureBound);

                if (pressureDelta > 0 && pipe.Air.Temperature > 0)
                {
                    var transferMoles = pressureDelta * environment.Air.Volume / (pipe.Air.Temperature * Atmospherics.R);

                    environment.AssumeAir(pipe.Air.Remove(transferMoles));
                }
            }
            else if (PumpDirection == VentPumpDirection.Siphoning && environment.Air.Pressure > 0)
            {
                var ourMultiplier = pipe.Air.Volume / (environment.Air.Temperature * Atmospherics.R);
                var molesDelta = 10000f * ourMultiplier;

                if ((PressureChecks & VentPressureBound.ExternalBound) != 0)
                    molesDelta = MathF.Min(molesDelta,
                        (environment.Air.Pressure - ExternalPressureBound) * environment.Air.Volume /
                        (environment.Air.Temperature * Atmospherics.R));

                if ((PressureChecks & VentPressureBound.InternalBound) != 0)
                    molesDelta = MathF.Min(molesDelta, (InternalPressureBound - pipe.Air.Pressure) * ourMultiplier);

                if (molesDelta > 0)
                {
                    var removed = environment.Air.Remove(molesDelta);
                    pipe.Air.Merge(removed);
                    environment.Invalidate();
                }
            }
        }
    }

    public enum VentPumpDirection : sbyte
    {
        Siphoning = 0,
        Releasing = 1,
    }

    [Flags]
    public enum VentPressureBound : sbyte
    {
        NoBound       = 0,
        InternalBound = 1,
        ExternalBound = 2,
    }
}
