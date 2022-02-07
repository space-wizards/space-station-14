using System;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public class GasDualPortVentPumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Welded { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;

        [ViewVariables(VVAccess.ReadWrite)]
        public DualPortVentPressureBound PressureChecks { get; set; } = DualPortVentPressureBound.ExternalBound;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ExternalPressureBound { get; set; } = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        public float InputPressureMin { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float OutputPressureMax { get; set; } = 0f;
    }

    [Flags]
    public enum DualPortVentPressureBound : sbyte
    {
        NoBound       = 0,
        ExternalBound = 1,
        InputMinimum = 2,
    }
}
