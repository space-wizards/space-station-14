using System;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IGridAtmosphereComponent))]
    [ComponentReference(typeof(GridAtmosphereComponent))]
    [Serializable]
    public class UnsimulatedGridAtmosphereComponent : GridAtmosphereComponent, IGridAtmosphereComponent
    {
        public override string Name => "UnsimulatedGridAtmosphere";

        public override bool Simulated => false;
    }
}
