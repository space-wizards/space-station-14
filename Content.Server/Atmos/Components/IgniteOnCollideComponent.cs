using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class IgniteOnCollideComponent : Component
    {
        public override string Name => "IgniteOnCollide";

        [DataField("fireStacks")]
        public float FireStacks { get; set; } 
    }
}
