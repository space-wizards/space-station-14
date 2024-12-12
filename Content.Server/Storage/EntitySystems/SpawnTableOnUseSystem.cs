using Content.Server.Administration.Logs;
using Content.Server.Storage.Components;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;

namespace Content.Server.Storage.EntitySystems;

public sealed class SpawnTableOnUseSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnTableOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<SpawnTableOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var coords = Transform(ent).Coordinates;
        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        foreach (var id in spawns)
        {
            var spawned = Spawn(id, coords);
            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User):user} used {ToPrettyString(ent):spawner} which spawned {ToPrettyString(spawned)}");
            _hands.TryPickupAnyHand(args.User, spawned);
        }

        Del(ent);
    }
}
