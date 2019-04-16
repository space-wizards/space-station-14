using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum SmesVisuals
    {
        LastChargeState,
        LastChargeLevel,
    }
}
