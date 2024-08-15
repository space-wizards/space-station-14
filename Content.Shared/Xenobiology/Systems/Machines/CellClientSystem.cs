using Content.Shared.Xenobiology.Components.Machines;

namespace Content.Shared.Xenobiology.Systems.Machines;

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
}
