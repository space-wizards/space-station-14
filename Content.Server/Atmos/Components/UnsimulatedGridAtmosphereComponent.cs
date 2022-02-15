using System;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IAtmosphereComponent))]
    [Serializable]
    public class UnsimulatedGridAtmosphereComponent : GridAtmosphereComponent
    {
        public override bool Simulated => false;
    }
}
