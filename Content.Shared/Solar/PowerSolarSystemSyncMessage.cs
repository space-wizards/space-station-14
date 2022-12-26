using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Solar
{
    [Serializable, NetSerializable]
    public sealed class PowerSolarSystemSyncMessage : EntityEventArgs
    {
        public Angle Angle;
        public Angle AngularVelocity;
        public PowerSolarSystemSyncMessage(Angle angle, Angle angularVelocity) {
            Angle = angle;
            AngularVelocity = angularVelocity;
        }
    }
}
