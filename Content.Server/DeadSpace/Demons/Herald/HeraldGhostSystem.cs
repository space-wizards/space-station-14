// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DeadSpace.Demons.Herald.Components;
using Content.Shared.Maps;
using Content.Shared.DeadSpace.Demons.Herald;
using Robust.Server.GameObjects;
using Content.Shared.Popups;
using Content.Shared.Physics;
using Content.Shared.Mind;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles;

namespace Content.Server.DeadSpace.Demons.Herald;

public sealed class HeraldGhostSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeraldGhostComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HeraldGhostComponent, HeraldSpawnActionEvent>(DoSpawn);
    }

    private void OnComponentInit(EntityUid uid, HeraldGhostComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionHeraldSpawnEntity, component.ActionHeraldSpawn, uid);
    }

    private void DoSpawn(EntityUid uid, HeraldGhostComponent component, HeraldSpawnActionEvent args)
    {
        if (args.Handled)
            return;

        var tileref = Transform(uid).Coordinates.GetTileRef();
        if (tileref != null)
        {
            if (_physics.GetEntitiesIntersectingBody(uid, (int)CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return;
            }
        }

        args.Handled = true;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var ent = Spawn(component.HeraldMobSpawnId, Transform(uid).Coordinates);

        if (!EntityManager.TryGetComponent<GhostRoleComponent>(ent, out var ghostRoleComponent))
        {
            _mindSystem.TransferTo(mindId, ent);
            Spawn(component.DemonPortalSpawnId, Transform(uid).Coordinates);
            QueueDel(uid);

            return;
        }

        var id = ghostRoleComponent.Identifier;
        var session = mind.Session; // Store session in a local variable

        if (session != null)
        {
            EntityManager.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(session, id);
        }
        else
        {
            return;
        }

        Spawn(component.DemonPortalSpawnId, Transform(uid).Coordinates);

        QueueDel(uid);
    }
}
