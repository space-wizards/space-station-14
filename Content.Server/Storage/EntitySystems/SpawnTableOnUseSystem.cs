using Content.Server.Administration.Logs;
using Content.Server.Storage.Components;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Storage.EntitySystems;

public sealed class SpawnTableOnUseSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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
            _hands.TryPickupAnyHand(args.User, spawned);
        }

        _audio.PlayPvs(ent.Comp.Sound, coords);
        Del(ent);
    }
}
