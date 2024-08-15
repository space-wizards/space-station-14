using Content.Shared.Xenobiology.Components.Machines;

namespace Content.Shared.Xenobiology.Events;

public sealed class CellServerDatabaseChangedEvent : EntityEventArgs
{
    public readonly Entity<CellServerComponent> Server;
    public readonly Entity<CellClientComponent> Client;

    public CellServerDatabaseChangedEvent(Entity<CellServerComponent> server, Entity<CellClientComponent> client)
    {
        Server = server;
        Client = client;
    }
}
