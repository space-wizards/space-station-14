using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum ChargeState
    {
        Still,
        Charging,
        Discharging,
    }
}
