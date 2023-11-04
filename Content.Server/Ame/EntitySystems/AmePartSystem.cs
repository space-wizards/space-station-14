using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Ame.Components;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Server.Ame.EntitySystems;

public sealed class AmePartSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmePartComponent, InteractUsingEvent>(OnPartInteractUsing);
    }

    private void OnPartInteractUsing(EntityUid uid, AmePartComponent component, InteractUsingEvent args)
    {
        if (!_toolSystem.HasQuality(args.Used, component.QualityNeeded))
            return;

        if (!_mapManager.TryGetGrid(args.ClickLocation.GetGridUid(EntityManager), out var mapGrid))
            return; // No AME in space.

        var snapPos = mapGrid.TileIndicesFor(args.ClickLocation);
        if (mapGrid.GetAnchoredEntities(snapPos).Any(sc => HasComp<AmeShieldComponent>(sc)))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-part-component-shielding-already-present"), uid, args.User);
            return;
        }

        var ent = Spawn("AmeShielding", mapGrid.GridTileToLocal(snapPos));

        _adminLogger.Add(LogType.Construction, LogImpact.Low, $"{ToPrettyString(args.User):player} unpacked {ToPrettyString(ent)} at {Transform(ent).Coordinates} from {ToPrettyString(uid)}");

        _audioSystem.PlayPvs(component.UnwrapSound, uid);

        QueueDel(uid);
    }
}
