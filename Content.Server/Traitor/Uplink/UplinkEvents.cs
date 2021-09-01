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

    public class UplinkSetAccountEvent : HandledEntityEventArgs
    {
        public UplinkAccount Account;

        public UplinkSetAccountEvent(UplinkAccount account)
        {
            Account = account;
        }
    }

    public class ShowUplinkUIAttempt : CancellableEntityEventArgs
    {
        public IPlayerSession Session;

        public ShowUplinkUIAttempt(IPlayerSession session)
        {
            Session = session;
        }
    }
}
