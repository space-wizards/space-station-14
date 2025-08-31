using System;
using System.Numerics;
using Content.Shared.Directions;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Abilities.Goliath;

public sealed class GoliathTentacleSystem : DelayableEntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    protected override float Threshold { get; set; } = 0.30f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GoliathSummonTentacleAction>(OnSummonAction);
    }

    private void OnSummonAction(GoliathSummonTentacleAction args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (!TryComp(args.Performer, out TransformComponent? xform)) return;
        _popup.PopupPredicted(Loc.GetString("tentacle-ability-use-popup", ("entity", args.Performer)), args.Performer, args.Performer, type: PopupType.SmallCaution);
        _stun.TryAddStunDuration(args.Performer, TimeSpan.FromSeconds(0.8f));

        Queue<EntityCoordinates> spawnPos = new();
        var direction = Vector2.Normalize(args.Target.Position - xform.Coordinates.Position);
        var pos = xform.Coordinates;
        for (var i = 0; i < 9; i++)
        {
            pos = pos.Offset(direction);
            spawnPos.Enqueue(pos);
        }

        if (_transform.GetGrid(args.Target) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;
        void action(EntityCoordinates pos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef) ||
                _turf.IsSpace(tileRef) ||
                _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                return;

            if (_net.IsServer)
                Spawn(args.EntityId, pos);
            if ((Action<EntityCoordinates>?)action is not null && spawnPos.TryDequeue(out var newPos))
            {
                EnqueueNext(() => action(newPos));
            }
        }

        EnqueueNext(() => action(spawnPos.Dequeue()));
    }
}
