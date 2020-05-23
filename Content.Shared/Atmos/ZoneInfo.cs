using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Content.Shared.Atmos
{
    /// <summary>
    /// Representation of the current zone, as sent over the wire.
    /// </summary>
    /// <remarks>
    /// Currently used just for debugging.
    /// </remarks>
    [Serializable, NetSerializable]
    public class ZoneInfo : EntitySystemMessage
    {
        /// <summary>
        /// The grid coordinates which make up this zone.
        /// </summary>
        public MapIndices[] Cells { get; set; }

        /// <summary>
        /// The gases which are contained in this zone.
        /// </summary>
        public GasProperty[] Contents { get; set; }
    }
}
