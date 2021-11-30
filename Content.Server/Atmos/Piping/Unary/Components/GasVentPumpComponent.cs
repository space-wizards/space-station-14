using System;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public class GasVentPumpComponent : Component
    {
        public override string Name => "GasVentPump";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Welded { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPressureBound PressureChecks { get; set; } = VentPressureBound.ExternalBound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("externalPressureBound")]
        public float ExternalPressureBound { get; set; } = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        public float InternalPressureBound { get; set; } = 0f;
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
