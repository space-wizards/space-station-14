using Content.Server.Body.Components;
using Content.Server.Chat.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GenericAntag;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Terminator.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Terminator.Systems;

public sealed class TerminatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorRoleComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<TerminatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TerminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        SubscribeLocalEvent<TerminatorComponent, GenericAntagCreatedEvent>(OnCreated);
    }

    private void OnUnpaused(Entity<TerminatorRoleComponent> ent, ref EntityUnpausedEvent args)
    {
        if (ent.Comp.TerminationTime != null)
            ent.Comp.TerminationTime = ent.Comp.TerminationTime + args.PausedTime;
    }

    private void OnMapInit(EntityUid uid, TerminatorComponent comp, MapInitEvent args)
    {
        // cyborg doesn't need to breathe
        RemComp<RespiratorComponent>(uid);
    }

    private void OnSpawned(EntityUid uid, TerminatorComponent comp, GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<TerminatorTargetComponent>(args.Spawner, out var target))
            return;

        comp.Target = target.Target;
    }

    private void OnCreated(EntityUid uid, TerminatorComponent comp, ref GenericAntagCreatedEvent args)
    {
        var mindId = args.MindId;
        var mind = args.Mind;

        _role.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = Loc.GetString("terminator-role-briefing")
        }, mind);
        _role.MindAddRole(mindId, new TerminatorRoleComponent(), mind);
    }

    /// <summary>
    /// Create a spawner at a position and return it.
    /// </summary>
    /// <param name="coords">Coordinates to create the spawner at</param>
    /// <param name="target">Optional target mind to force the terminator to target</param>
    public EntityUid CreateSpawner(EntityCoordinates coords, EntityUid? target)
    {
        var uid = Spawn("SpawnPointGhostTerminator", coords);
        if (target != null)
        {
            var comp = EnsureComp<TerminatorTargetComponent>(uid);
            comp.Target = target;
        }

        return uid;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TerminatorRoleComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var comp, out var mindComp))
        {

            if (comp.TerminationTime != null)
            {
                if (mindComp.OwnedEntity is not { } terminator)
                    continue;

                if (_timing.CurTime > comp.TerminationTime)
                {
                    _explosion.TriggerExplosive(terminator);
                }

                continue;
            }

            if (!_objectives.AllObjectivesComplete((uid, mindComp)))
                continue;

            if (mindComp.Session != null)
            {
                var msg = Loc.GetString("terminator-mission-accomplished", ("second", comp.TerminationDelay.TotalSeconds));
                _chatManager.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, mindComp.Session.Channel);
            }

            comp.TerminationTime = _timing.CurTime + comp.TerminationDelay;
        }
    }
}
