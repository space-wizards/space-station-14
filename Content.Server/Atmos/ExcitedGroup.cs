using System;
using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public class ExcitedGroup
    {
        [ViewVariables]
        public bool Disposed = false;

        [ViewVariables]
        public readonly HashSet<TileAtmosphere> Tiles = new();

        [ViewVariables]
        public int DismantleCooldown { get; set; }

        [ViewVariables]
        public int BreakdownCooldown { get; set; }
    }
}
