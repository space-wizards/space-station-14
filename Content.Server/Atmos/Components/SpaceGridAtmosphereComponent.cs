using System.Collections.Generic;
using System.Linq;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IGridAtmosphereComponent))]
    public class SpaceGridAtmosphereComponent : UnsimulatedGridAtmosphereComponent
    {
        public override string Name => "SpaceGridAtmosphere";
    }
}
