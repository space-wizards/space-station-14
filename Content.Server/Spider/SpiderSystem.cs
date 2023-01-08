using System.Linq;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.Spider;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.MobState.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spider
{
    public sealed class SpiderSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpiderWebObjectComponent, ComponentStartup>(OnWebStartup);
            SubscribeLocalEvent<SpiderComponent, ComponentStartup>(OnSpiderStartup);
            SubscribeLocalEvent<SpiderComponent, SpiderWebActionEvent>(OnSpawnNet);
        }

        private void OnSpiderStartup(EntityUid uid, SpiderComponent component, ComponentStartup args)
        {
            var netAction = new InstantAction(_proto.Index<InstantActionPrototype>("SpiderWebAction"));
            _action.AddAction(uid, netAction, null);
        }

        private void OnWebStartup(EntityUid uid, SpiderWebObjectComponent component, ComponentStartup args)
        {
            _appearance.SetData(uid,SpiderWebVisuals.Variant, _robustRandom.Next(1, 3));
        }

        private void OnSpawnNet(EntityUid uid, SpiderComponent component, SpiderWebActionEvent args)
        {
            if (args.Handled)
                return;

            var transform = Transform(uid);

            if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
                return;

            args.Handled = true;

            var coords = transform.Coordinates;

            // Spawn web in center
            if (!IsTileBlockedByWeb(coords))
                Spawn(component.WebPrototype, coords);  

            // Spawn web in other directions
            for (var i = 0; i < 4; i++)
            {
                var direction = (DirectionFlag) (1 << i);
                coords = transform.Coordinates.Offset(direction.AsDir().ToVec());
                    
                if (!IsTileBlockedByWeb(coords))
                    Spawn(component.WebPrototype, coords);
            }
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
}

