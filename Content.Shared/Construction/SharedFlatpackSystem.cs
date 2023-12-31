using System.Numerics;
using Content.Shared.Construction.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Construction;

public sealed class SharedFlatpackSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FlatpackComponent, InteractUsingEvent>(OnFlatpackInteractUsing);
        SubscribeLocalEvent<FlatpackComponent, ExaminedEvent>(OnFlatpackExamined);
    }

    private void OnFlatpackInteractUsing(Entity<FlatpackComponent> ent, ref InteractUsingEvent args)
    {
        var (uid, comp) = ent;
        if (!_tool.HasQuality(args.Used, comp.QualityNeeded))
            return;

        var xform = Transform(ent);

        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        args.Handled = true;

        if (comp.Entity == null)
        {
            Log.Error($"No entity prototype present for flatpack {ToPrettyString(ent)}.");

            if (_net.IsServer)
                QueueDel(ent);
            return;
        }

        var buildPos = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var intersecting = _entityLookup.GetEntitiesIntersecting(buildPos.ToEntityCoordinates(grid, _mapManager).Offset(new Vector2(0.5f, 0.5f)),
            LookupFlags.Dynamic | LookupFlags.Static);

        // todo make this logic smarter.
        // This should eventually allow for shit like building microwaves on tables and such.
        foreach (var intersect in intersecting)
        {
            if (!TryComp<PhysicsComponent>(intersect, out var intersectBody))
                continue;

            if (!intersectBody.Hard || !intersectBody.CanCollide)
                continue;

            _popup.PopupClient(Loc.GetString("flatpack-unpack-no-room"), uid, args.User);
            return;
        }

        if (_net.IsServer)
        {
            var spawn = Spawn(comp.Entity, _map.GridTileToLocal(grid, gridComp, buildPos));
            _adminLogger.Add(LogType.Construction, LogImpact.Low,
                $"{ToPrettyString(args.User):player} unpacked {ToPrettyString(spawn):entity} at {xform.Coordinates} from {ToPrettyString(uid):entity}");
            QueueDel(uid);
        }

        _audio.PlayPredicted(comp.UnpackSound, args.Used, args.User);
    }

    private void OnFlatpackExamined(Entity<FlatpackComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        args.PushMarkup(Loc.GetString("flatpack-examine"));
    }
}
