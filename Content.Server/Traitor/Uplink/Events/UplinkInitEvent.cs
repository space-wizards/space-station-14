using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;

namespace Content.Server.Traitor.Uplink.Events
{
    public class UplinkInitEvent : EntityEventArgs
    {
        public UplinkComponent uplink;

        public UplinkInitEvent(UplinkComponent uplink)
        {
            this.uplink = uplink;
        }
    }
}
