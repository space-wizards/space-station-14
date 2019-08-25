using System.Collections.Generic;
using Content.Shared.GameObjects.Components;

namespace Content.Client.GameObjects.Components
{
    public class WiresComponent : SharedWiresComponent
    {
        public List<ClientWire> ClientWiresList = new List<ClientWire>();
    }
}
