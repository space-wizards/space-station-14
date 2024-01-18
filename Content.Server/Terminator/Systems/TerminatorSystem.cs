using Content.Server.Body.Components;
using Content.Server.GenericAntag;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Station;
using Content.Shared.Terminator.Components;
using Content.Shared.Terminator.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Terminator.Systems;

public sealed class TerminatorSystem : SharedTerminatorSystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedStationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        SubscribeLocalEvent<TerminatorComponent, GenericAntagCreatedEvent>(OnCreated);
        SubscribeLocalEvent<TerminatorComponent, ExterminatorCurseEvent>(OnCursed);
    }

    protected override void OnMapInit(Entity<TerminatorComponent> ent, ref MapInitEvent args)
    {
        base.OnMapInit(ent, ref args);

        // cyborg doesn't need to breathe
        RemComp<RespiratorComponent>(ent);
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

    private void OnCursed(Entity<TerminatorComponent> ent, ref ExterminatorCurseEvent args)
    {
        // should only happen with map loading funnies but better safe than sorry
        if (HasComp<ReplacementAccentComponent>(ent) || ent.Comp.CurseActionEntity is not {} action)
            return;

        // give arnie gear
        var gear = _proto.Index<StartingGearPrototype>(ent.Comp.CurseGear);
        _stationSpawning.EquipStartingGear(ent, gear, profile: null);

        // apply the curse...
        EnsureComp<ReplacementAccentComponent>(ent).Accent = ent.Comp.CurseAccent;

        _popup.PopupEntity(Loc.GetString("terminator-curse-popup"), ent, ent, PopupType.LargeCaution);

        // no more action
        _actionContainer.RemoveAction(action);
        ent.Comp.CurseActionEntity = null;
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
