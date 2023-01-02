using System.Linq;
using Content.Server.Actions;
using Content.Server.Atmos.Components;
//using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Atmos;
using Content.Shared.MobState.Components;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Spider
{
    public sealed class SpiderSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpiderComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SpiderComponent, SpiderNetActionEvent>(OnSpawnNet);
        }

        private void OnStartup(EntityUid uid, SpiderComponent component, ComponentStartup args)
        {
            var netAction = new InstantAction(_proto.Index<InstantActionPrototype>("SpiderNetAction"));
            _action.AddAction(uid, netAction, null);
        }

        private void OnSpawnNet(EntityUid uid, SpiderComponent component, SpiderNetActionEvent args)
        {
            if (args.Handled)
                return;

            var transform = Transform(uid);

            if (!_mapManager.TryGetGrid(transform.GridUid, out var grid)) return;

            args.Handled = true;

            bool notBlocked = false;
            var coords = transform.Coordinates;
            if (!grid.GetTileRef(coords).Tile.IsEmpty)
            {
                var ents = grid.GetLocal(coords);
                bool net = false;
                if (ents.All(x => !IsTileBlocked(x,ref net)))
                {
                    notBlocked = true;
                    if (!net)
                        EntityManager.SpawnEntity("SpiderNet", transform.Coordinates);  
                }
            }

            if (notBlocked)
            {
                for (var i = 0; i < 4; i++)
                {
                    var direction = (DirectionFlag) (1 << i);
                    coords = transform.Coordinates.Offset(direction.AsDir().ToVec());
                    if (grid.GetTileRef(coords).Tile.IsEmpty) continue;
                    var ents = grid.GetLocal(coords);

                    if (ents.Any(x => IsTileBlockedFrom(x, direction))) continue;

                    EntityManager.SpawnEntity("SpiderNet", transform.Coordinates.Offset(direction.AsDir().ToVec()));
                }
            }
        }

        private bool IsTileBlocked(EntityUid ent, ref bool net)
        {
            if (HasComp<SpiderNetObjectComponent>(ent))
                net = true;
            if (!EntityManager.TryGetComponent<AirtightComponent>(ent, out var airtight))
                return false;
            return airtight.AirBlocked;
        }

        private bool IsTileBlockedFrom(EntityUid ent, DirectionFlag dir)
        {
            if (HasComp<SpiderNetObjectComponent>(ent))
                return true;
            if (!EntityManager.TryGetComponent<AirtightComponent>(ent, out var airtight))
                return false;

            var oppositeDir = dir.AsDir().GetOpposite().ToAtmosDirection();

            return airtight.AirBlocked && airtight.AirBlockedDirection.IsFlagSet(oppositeDir);
        }
    }
}

public sealed class SpiderNetActionEvent : InstantActionEvent { };

public sealed class SpiderJumpActionEvent : WorldTargetActionEvent { };
