using Content.Server.Body.Components;
using Content.Server.GenericAntag;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Exterminator.Components;
using Content.Shared.Exterminator.Systems;
using Robust.Shared.Map;

namespace Content.Server.Exterminator.Systems;

public sealed class ExterminatorSystem : SharedExterminatorSystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExterminatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ExterminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        SubscribeLocalEvent<ExterminatorComponent, GenericAntagCreatedEvent>(OnCreated);
    }

    private void OnMapInit(Entity<ExterminatorComponent> ent, ref MapInitEvent args)
    {
        // cyborg doesn't need to breathe
        // TODO: something else (probably means supporting prototypes that remove components)
        RemComp<RespiratorComponent>(ent);
    }

    private void OnSpawned(Entity<ExterminatorComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<ExterminatorTargetComponent>(args.Spawner, out var target))
            return;

        // objective will check this
        EnsureComp<ExterminatorTargetComponent>(ent).Target = target.Target;
    }

    private void OnCreated(Entity<ExterminatorComponent> ent, ref GenericAntagCreatedEvent args)
    {
        var mindId = args.MindId;
        var mind = args.Mind;

        _role.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = Loc.GetString("exterminator-role-briefing")
        }, mind);
        _role.MindAddRole(mindId, new ExterminatorRoleComponent(), mind);
    }

    /// <summary>
    /// Create a spawner at a position and return it.
    /// </summary>
    /// <param name="coords">Coordinates to create the spawner at</param>
    /// <param name="target">Optional target mind to force the exterminator to target</param>
    public EntityUid CreateSpawner(EntityCoordinates coords, EntityUid? target)
    {
        var uid = Spawn("SpawnPointGhostExterminator", coords);
        if (target != null)
        {
            var comp = EnsureComp<ExterminatorTargetComponent>(uid);
            comp.Target = target;
        }

        return uid;
    }
}
