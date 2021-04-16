using System;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer.GhostRoles;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Tickets
{
    [NetSerializable, Serializable]
    public class TicketEuiState : EuiStateBase
    {
        public Ticket? ticket { get; }

        public TicketEuiState(Ticket? _ticket)
        {
            ticket = _ticket;
        }
    }
}

