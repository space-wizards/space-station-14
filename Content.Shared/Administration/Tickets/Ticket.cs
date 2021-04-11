using System.Collections.Generic;
using Robust.Shared.Network;

namespace Content.Shared.Administration.Tickets
{
    public class Ticket
    {
        public NetUserId? TargetPlayer { get; set; }

        public NetUserId? ClaimedAdmin { get; set; }

        public int Id { get; set; }

        public bool TicketOpen = true;

        public List<string> Messages = new();

        public Ticket(int id, NetUserId opener, NetUserId? target, string message)
        {
            Id = id;
            if (target != null)
            {
                TargetPlayer = target;
                ClaimedAdmin = opener;
            }
            else
            {
                TargetPlayer = opener;
            }
            Messages.Add(message);
        }
    }
}
