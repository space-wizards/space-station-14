using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Security
{
    [Serializable, NetSerializable]
    public enum DeployableBarrierVisuals : byte
    {
        State
    }


    [Serializable, NetSerializable]
    public enum DeployableBarrierState : byte
    {
        Idle,
        Deployed
    }
}
