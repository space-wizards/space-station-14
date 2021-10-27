using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.Traitor.Uplink
{
    public class UplinkInitEvent : EntityEventArgs
    {
        public UplinkComponent Uplink;

        public UplinkInitEvent(UplinkComponent uplink)
        {
            Uplink = uplink;
        }
    }

    public class UplinkRemovedEvent : EntityEventArgs
    {
    }
}
