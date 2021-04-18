using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Administration.Tickets
{
    [NetSerializable, Serializable]
    public class Ticket
    {
        public NetUserId TargetPlayer { get; set; }
        public string PlayerUsername = string.Empty;

        public NetUserId? ClaimedAdmin { get; set; }
        public string AdminUsername = string.Empty;

        public int Id { get; set; }

        public bool TicketOpen = true;

        public string? Character { get; set; }

        public TicketStatus Status = TicketStatus.Unclaimed;

        public List<TicketMessage> Messages = new();

        public Ticket(int id, NetUserId opener, NetUserId target, string message)
        {
            Id = id;
            if (opener != target)
            {
                TargetPlayer = target;
                ClaimedAdmin = opener;
                Status = TicketStatus.Claimed;
            }
            else
            {
                TargetPlayer = opener;
            }

            var time = DateTimeOffset.Now;
            var msg = new TicketMessage(time.Ticks, time.Offset.Ticks, opener.ToString(), opener != target, message);
            Messages.Add(msg);
        }

        public string GetPlayerName()
        {
            return string.IsNullOrEmpty(PlayerUsername) ? TargetPlayer.ToString() : PlayerUsername;
        }

        public string GetAdminName()
        {
            return string.IsNullOrEmpty(AdminUsername) ? ClaimedAdmin?.ToString() ?? "Unclaimed" : AdminUsername;
        }
    }

    [NetSerializable, Serializable]
    public record TicketMessage(long time, long offset, string author, bool admin, string message);
}
