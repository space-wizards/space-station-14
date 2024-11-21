using System.Diagnostics.CodeAnalysis;
using Content.Shared.Xenobiology.Components.Server;

namespace Content.Shared.Xenobiology.Systems.Machines.Connection;

public sealed class CellClientSystem : EntitySystem
{
    [Dependency] private readonly CellServerSystem _cellServer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellClientComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<CellClientComponent> ent, ref ComponentInit args)
    {
        foreach (var server in _cellServer.GetServers())
        {
            _cellServer.RegisterClient((server, server.Comp), (ent, ent.Comp));
        }
    }

    public bool TryGetCells(Entity<CellClientComponent?> ent, out IReadOnlyList<Cell> cells)
    {
        cells = [];

        if (!TryGetServer(ent, out var server))
            return false;

        cells = server.Value.Comp.Cells;
        return true;
    }

    public bool TryGetServer(Entity<CellClientComponent?> ent, [NotNullWhen(true)] out Entity<CellServerComponent>? serverEnt)
    {
        serverEnt = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.ConnectedToServer)
            return false;

        if (!TryComp<CellServerComponent>(ent.Comp.Server!.Value, out var serverComponent))
            return false;

        serverEnt = (ent.Comp.Server!.Value, serverComponent);
        return true;
    }
}
