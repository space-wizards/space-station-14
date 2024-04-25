using Content.Server.Body.Components;
using Content.Server.GenericAntag;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Roles;
using Content.Server.Terminator.Components;
using Content.Shared.Roles;
using Robust.Shared.Map;

namespace Content.Server.Terminator.Systems;

public sealed class TerminatorSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TerminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        SubscribeLocalEvent<TerminatorComponent, GenericAntagCreatedEvent>(OnCreated);
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
}
