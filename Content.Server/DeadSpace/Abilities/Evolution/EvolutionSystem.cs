using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.DeadSpace.Abilities.Evolution.Components;
using Content.Shared.DeadSpace.Abilities.Evolution;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Zombies;
using Content.Shared.DeadSpace.EntityPanel;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Abilities.Evolution;

public sealed class EvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvolutionComponent, ReadyEvolutionEvent>(OnReadyEvolution);
        SubscribeLocalEvent<EvolutionComponent, EvolutionDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EvolutionComponent, EvolutionActionEvent>(OnSelectEntityAction);
        SubscribeLocalEvent<EvolutionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EvolutionComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<EvolutionComponent, EntityZombifiedEvent>(OnZombification);
        SubscribeLocalEvent<EvolutionComponent, InfectionNecroficationEvent>(OnNecrofication);

        SubscribeNetworkEvent<SelectEntityEvent>(OnSelectEntity);
    }

    private void OnReadyEvolution(EntityUid uid, EvolutionComponent component, ReadyEvolutionEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.EvolutionActionEntity, component.EvolutionAction, uid);

        if (!component.CreateGhostRole)
            return;

        if (!HasComp<GhostRoleComponent>(uid))
        {
            var ghostRole = AddComp<GhostRoleComponent>(uid);

            ghostRole.RoleName = component.GhostRoleName;
            ghostRole.RoleDescription = component.GhostRoleDesk;

            if (!HasComp<GhostTakeoverAvailableComponent>(uid))
                AddComp<GhostTakeoverAvailableComponent>(uid);
        }
    }

    private void OnShutdown(EntityUid uid, EvolutionComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.EvolutionActionEntity);
    }

    private void OnZombification(EntityUid uid, EvolutionComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.EvolutionActionEntity);
    }

    private void OnNecrofication(EntityUid uid, EvolutionComponent component, InfectionNecroficationEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.EvolutionActionEntity);
    }

    private void OnExamine(EntityUid uid, EvolutionComponent component, ExaminedEvent args)
    {
        var time = _timing.CurTime - component.TimeUntilEvolution;
        double seconds = Math.Abs(time.TotalSeconds);
        int roundedSeconds = (int)Math.Round(seconds);

        if (args.Examiner == args.Examined)
        {
            if (time.TotalSeconds < 0)
            {
                args.PushMarkup(Loc.GetString($"Вы будете готовы к эволюции через [color=green]{roundedSeconds} секунд[/color]."));
            }
            else
            {
                args.PushMarkup(Loc.GetString($"Вы готовы эвалюционировать."));
            }
        }
    }

    private void OnSelectEntityAction(EntityUid uid, EvolutionComponent component, EvolutionActionEvent args)
    {
        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestEntityMenuEvent(uid.Id, true, false);

            foreach (var entityUid in component.SpawnedEntities)
            {
                ev.Prototypes.Add(entityUid);
            }
            ev.Prototypes.Sort();
            RaiseNetworkEvent(ev, actorComponent.PlayerSession);
        }

        args.Handled = true;
    }

    private void OnSelectEntity(SelectEntityEvent msg)
    {
        if (msg.IsUseEvolutionSystem)
        {
            if (EntityManager.TryGetComponent<EvolutionComponent>(new EntityUid(msg.Target), out var evolutionComponent))
            {
                evolutionComponent.SelectEntity = msg.PrototypeId;
                OnEvolutionAction(new EntityUid(msg.Target), evolutionComponent);
            }
        }
    }

    private void OnEvolutionAction(EntityUid uid, EvolutionComponent comp)
    {
        BeginSpawn(uid, comp);
    }

    private void BeginSpawn(EntityUid uid, EvolutionComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(5f), new EvolutionDoAfterEvent(), uid)
        {
            DistanceThreshold = 3,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, EvolutionComponent component, EvolutionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var xform = Transform(uid);

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            return;
        }

        var ent = Spawn(component.SelectEntity, Transform(uid).Coordinates);

        if (!EntityManager.TryGetComponent<GhostRoleComponent>(ent, out var ghostRoleComponent))
        {
            _mindSystem.TransferTo(mindId, ent);
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

        QueueDel(uid);
    }
}
