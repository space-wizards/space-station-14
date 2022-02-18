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
    public sealed class IgniteOnCollideComponent : Component
    {
        [DataField("fireStacks")]
        public float FireStacks { get; set; }
    }
}
