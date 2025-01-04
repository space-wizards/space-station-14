using System.Linq;
using Content.Shared.Xenobiology.Components.Server;
using Content.Shared.Xenobiology.Events;

namespace Content.Shared.Xenobiology.Systems.Machines.Connection;

public sealed class CellServerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CellServerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CellServerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Id = GenerateId();
        Dirty(ent);
    }

    private void OnShutdown(Entity<CellServerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var client in ent.Comp.Clients)
        {
            UnregisterClient((ent, ent), client);
        }
    }

    public IEnumerable<Entity<CellServerComponent>> GetServers()
    {
        var query = EntityQueryEnumerator<CellServerComponent>();
        while (query.MoveNext(out var uid, out var serverComponent))
        {
            yield return (uid, serverComponent);
        }
    }

    public void RegisterClient(Entity<CellServerComponent?> server, Entity<CellClientComponent?> client)
    {
        if (!Resolve(server, ref server.Comp) || !Resolve(client, ref client.Comp))
            return;

        server.Comp.Clients.Add(client);
        client.Comp.Server = server;
        Dirty(server);
    }

    public void UnregisterClient(Entity<CellServerComponent?> server, Entity<CellClientComponent?> client)
    {
        if (!Resolve(server, ref server.Comp) || !Resolve(client, ref client.Comp))
            return;

        server.Comp.Clients.Remove(client);
        client.Comp.Server = null;
        Dirty(server);
    }

    public bool HasClient(Entity<CellServerComponent?> server, Entity<CellClientComponent?> client)
    {
        if (!Resolve(server, ref server.Comp) || !Resolve(client, ref client.Comp))
            return false;

        return server.Comp.Clients.Contains(client);
    }

    public bool AddCell(EntityUid clientUid, Cell cell)
    {
        return AddCell((clientUid, null), cell);
    }

    public bool AddCell(Entity<CellClientComponent?> client, Cell cell)
    {
        if (!Resolve(client, ref client.Comp) || client.Comp.Server is null)
            return false;

        return AddCell((client.Comp.Server.Value, null), client, cell);
    }

    public bool AddCell(Entity<CellServerComponent?> server, Entity<CellClientComponent?> client, Cell cell)
    {
        if (!Resolve(server, ref server.Comp) || !Resolve(client, ref client.Comp))
            return false;

        if (!HasClient(server, client))
            return false;

        server.Comp.Cells.Add(cell);
        DispatchChangedEvent(server, client);

        return true;
    }

    public void RemoveCell(Entity<CellServerComponent?> server, Entity<CellClientComponent?> client, Cell cell)
    {
        if (!Resolve(server, ref server.Comp) || !Resolve(client, ref client.Comp))
            return;

        if (!HasClient(server, client))
            return;

        server.Comp.Cells.Remove(cell);
        Dirty(server);

        DispatchChangedEvent(server, client);
    }

    private void DispatchChangedEvent(Entity<CellServerComponent?> server, Entity<CellClientComponent?> client)
    {
        if (!Resolve(server, ref server.Comp) || !Resolve(client, ref client.Comp))
            return;

        var ev = new CellServerDatabaseChangedEvent((server, server.Comp), (client, client.Comp));
        RaiseLocalEvent(ev);
    }

    private int GenerateId()
    {
        return EntityQuery<CellServerComponent>(true).Max(server => server.Id) + 1;
    }
}
