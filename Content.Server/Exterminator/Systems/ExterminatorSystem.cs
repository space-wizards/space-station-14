using Content.Server.Body.Components;
using Content.Server.GenericAntag;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Station;
using Content.Shared.Exterminator.Components;
using Content.Shared.Exterminator.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Exterminator.Systems;

public sealed class ExterminatorSystem : SharedExterminatorSystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedStationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExterminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        SubscribeLocalEvent<ExterminatorComponent, GenericAntagCreatedEvent>(OnCreated);
        SubscribeLocalEvent<ExterminatorComponent, ExterminatorCurseEvent>(OnCursed);
    }

    protected override void OnMapInit(Entity<ExterminatorComponent> ent, ref MapInitEvent args)
    {
        base.OnMapInit(ent, ref args);

        // cyborg doesn't need to breathe
        // TODO: something else
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

    private void OnCursed(Entity<ExterminatorComponent> ent, ref ExterminatorCurseEvent args)
    {
        // should only happen with map loading funnies but better safe than sorry
        if (HasComp<ReplacementAccentComponent>(ent) || ent.Comp.CurseActionEntity is not {} action)
            return;

        // give arnie gear
        var gear = _proto.Index<StartingGearPrototype>(args.Gear);
        _stationSpawning.EquipStartingGear(ent, gear, profile: null);

        // apply the curse...
        EnsureComp<ReplacementAccentComponent>(ent).Accent = args.Accent;

        _popup.PopupEntity(Loc.GetString("exterminator-curse-popup"), ent, ent, PopupType.LargeCaution);

        // no more action
        _actionContainer.RemoveAction(action);
        ent.Comp.CurseActionEntity = null;
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
