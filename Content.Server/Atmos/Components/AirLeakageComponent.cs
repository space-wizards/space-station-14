using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent, Access(typeof(AirLeakageSystem))]
    public sealed partial class AirLeakageComponent : Component
    {
        /// <summary>
        /// The value the object tile's TransferRatio should be set to, slowing down atmos transfer through in/out of the tile.  
        /// </summary>
        [DataField]
        public float AirLeakageRatio = 1.0f;

        public (EntityUid Grid, Vector2i Tile) LastPosition { get; set; }
    }
}
