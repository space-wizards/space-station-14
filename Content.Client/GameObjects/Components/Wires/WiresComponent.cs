using System.Collections.Generic;
using Content.Shared.GameObjects.Components;

namespace Content.Client.GameObjects.Components
{
    public class WiresComponent : SharedWiresComponent
    {
        public List<ClientWiresListEntry> ClientWiresList = new List<ClientWiresListEntry>();
    }
}
