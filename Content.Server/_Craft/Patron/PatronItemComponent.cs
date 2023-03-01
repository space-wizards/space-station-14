using Content.Shared.Item;
using Robust.Server.Player;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Patron
{
    [RegisterComponent]
    [Access(typeof(PatronSystem))]
    public sealed class PatronItemComponent : Component
    {
        [DataField("enabled")]
        public bool Enabled = true;

        [DataField("patronOwner")]
        public IPlayerSession Patron = default!;
    }
}
