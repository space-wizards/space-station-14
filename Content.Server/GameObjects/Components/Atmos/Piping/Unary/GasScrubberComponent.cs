using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Unary
{
    [RegisterComponent]
    public class GasScrubberComponent : Component, IAtmosProcess
    {
        public override string Name => "GasScrubber";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        private string _outletName = "pipe";

        [ViewVariables]
        private readonly HashSet<Gas> _filterGases = new()
        {
            Gas.CarbonDioxide
        };

        [ViewVariables(VVAccess.ReadWrite)]
        public ScrubberPumpDirection PumpDirection { get; private set; } = ScrubberPumpDirection.Scrubbing;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeRate { get; private set; } = 200f;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool WideNet { get; private set; } = false;

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_outletName, out PipeNode? outlet))
                return;

            var environment = atmosphere.GetTile(Owner.Transform.Coordinates)!;

            Scrub(environment, outlet);

            if (!WideNet) return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in environment.AdjacentTiles)
            {
                Scrub(adjacent, outlet);
            }
        }

        private void Scrub(TileAtmosphere? tile, PipeNode outlet)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile?.Air == null)
                return;

            // Cannot scrub if pressure too high.
            if (outlet.Air.Pressure >= 50 * Atmospherics.OneAtmosphere)
                return;

            if (PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                var transferMoles = MathF.Min(1f, (VolumeRate / tile.Air.Volume) * tile.Air.TotalMoles);

                // Take a gas sample.
                var removed = tile.Air.Remove(transferMoles);

                // Nothing left to remove from the tile.
                if (MathHelper.CloseTo(removed.TotalMoles, 0f))
                    return;

                removed.ScrubInto(outlet.Air, _filterGases);

                // Remix the gases.
                tile.AssumeAir(removed);
            }
            else if (PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                var transferMoles = tile.Air.TotalMoles * (VolumeRate / tile.Air.Volume);

                var removed = tile.Air.Remove(transferMoles);

                outlet.Air.Merge(removed);
                tile.Invalidate();
            }
        }
    }

    public enum ScrubberPumpDirection : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }
}
