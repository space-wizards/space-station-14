using Content.Server.Mind.Components;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Content.Server.Mind
{
    public sealed partial class Mind
    {
        public void Persistence_ReattachPlayer(MindComponent c, NetUserId userId)
        {
            OwnedComponent = c;
            ChangeOwningPlayer(userId);
        }
    }
}
