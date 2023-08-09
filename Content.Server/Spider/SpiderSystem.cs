using System.Linq;
using Content.Server.Popups;
using Content.Shared.Spider;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Spider;

public sealed class SpiderSystem : SharedSpiderSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderComponent, SpiderWebActionEvent>(OnSpawnNet);
    }

    private void OnSpawnNet(EntityUid uid, SpiderComponent component, SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);

        if (transform.GridUid == null)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-nogrid"), args.Performer, args.Performer);
            return;
        }

        var coords = transform.Coordinates;

        // TODO generic way to get certain coordinates

        var result = false;
        // Spawn web in center
        if (!IsTileBlockedByWeb(coords))
        {
            Spawn(component.WebPrototype, coords);
            result = true;
        }

        // Spawn web in other directions
        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag) (1 << i);
            coords = transform.Coordinates.Offset(direction.AsDir().ToVec());

            if (!IsTileBlockedByWeb(coords))
            {
                Spawn(component.WebPrototype, coords);
                result = true;
            }
        }

        if (result)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-success"), args.Performer, args.Performer);
            args.Handled = true;
        }
        else
            _popup.PopupEntity(Loc.GetString("spider-web-action-fail"), args.Performer, args.Performer);
    }

    private bool IsTileBlockedByWeb(EntityCoordinates coords)
    {
        foreach (var entity in coords.GetEntitiesInTile())
        {
            if (HasComp<SpiderWebObjectComponent>(entity))
                return true;
        }
        return false;
    }
}

