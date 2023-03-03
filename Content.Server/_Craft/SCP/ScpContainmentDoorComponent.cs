using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.SCP
{
    [RegisterComponent]
    public sealed class ScpContainmentDoorComponent : Component
    {
        [DataField("doorGroup")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string DoorGroup = String.Empty;

        [DataField("doorBlocked")]
        [ViewVariables(VVAccess.ReadOnly)]
        public bool DoorBlocked = false;
    }
}
