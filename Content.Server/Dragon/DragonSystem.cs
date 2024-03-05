using Content.Server.GenericAntag;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Actions;
using Content.Shared.Dragon;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Dragon;

public sealed partial class DragonSystem : EntitySystem
{
    [Dependency] private readonly CarpRiftsConditionSystem _carpRifts = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<CarpRiftsConditionComponent> _objQuery;

    /// <summary>
    /// Minimum distance between 2 rifts allowed.
    /// </summary>
    private const int RiftRange = 15;

    /// <summary>
    /// Radius of tiles
    /// </summary>
    private const int RiftTileRadius = 2;

    private const int RiftsAllowed = 3;

    public override void Initialize()
    {
        base.Initialize();

        _objQuery = GetEntityQuery<CarpRiftsConditionComponent>();

        SubscribeLocalEvent<DragonComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DragonComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DragonComponent, DragonSpawnRiftActionEvent>(OnSpawnRift);
        SubscribeLocalEvent<DragonComponent, RefreshMovementSpeedModifiersEvent>(OnDragonMove);
        SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DragonComponent, GenericAntagCreatedEvent>(OnCreated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DragonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.WeakenedAccumulator > 0f)
            {
                comp.WeakenedAccumulator -= frameTime;

                // No longer weakened.
                if (comp.WeakenedAccumulator < 0f)
                {
                    comp.WeakenedAccumulator = 0f;
                    _movement.RefreshMovementSpeedModifiers(uid);
                }
            }

            // At max rifts
            if (comp.Rifts.Count >= RiftsAllowed)
                continue;

            // If there's an active rift don't accumulate.
            if (comp.Rifts.Count > 0)
            {
                var lastRift = comp.Rifts[^1];

                if (TryComp<DragonRiftComponent>(lastRift, out var rift) && rift.State != DragonRiftState.Finished)
                {
                    comp.RiftAccumulator = 0f;
                    continue;
                }
            }

            comp.RiftAccumulator += frameTime;

            // Delete it, naughty dragon!
            if (comp.RiftAccumulator >= comp.RiftMaxAccumulator)
            {
                Roar(uid, comp);
                QueueDel(uid);
            }
        }
    }

    private void OnInit(EntityUid uid, DragonComponent component, MapInitEvent args)
    {
        Roar(uid, component);
        _actions.AddAction(uid, ref component.SpawnRiftActionEntity, component.SpawnRiftAction);
    }

    private void OnShutdown(EntityUid uid, DragonComponent component, ComponentShutdown args)
    {
        DeleteRifts(uid, false, component);
    }

    private void OnSpawnRift(EntityUid uid, DragonComponent component, DragonSpawnRiftActionEvent args)
    {
        if (component.Weakened)
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-weakened"), uid, uid);
            return;
        }

        if (component.Rifts.Count >= RiftsAllowed)
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-max"), uid, uid);
            return;
        }

        if (component.Rifts.Count > 0 && TryComp<DragonRiftComponent>(component.Rifts[^1], out var rift) && rift.State != DragonRiftState.Finished)
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-duplicate"), uid, uid);
            return;
        }

        var xform = Transform(uid);

        // Have to be on a grid fam
        if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-anchor"), uid, uid);
            return;
        }

        // cant stack rifts near eachother
        foreach (var (_, riftXform) in EntityQuery<DragonRiftComponent, TransformComponent>(true))
        {
            if (riftXform.Coordinates.InRange(EntityManager, xform.Coordinates, RiftRange))
            {
                _popup.PopupEntity(Loc.GetString("carp-rift-proximity", ("proximity", RiftRange)), uid, uid);
                return;
            }
        }

        // cant put a rift on solars
        foreach (var tile in grid.GetTilesIntersecting(new Circle(xform.WorldPosition, RiftTileRadius), false))
        {
            if (!tile.IsSpace(_tileDef))
                continue;

            _popup.PopupEntity(Loc.GetString("carp-rift-space-proximity", ("proximity", RiftTileRadius)), uid, uid);
            return;
        }

        var carpUid = Spawn(component.RiftPrototype, xform.MapPosition);
        component.Rifts.Add(carpUid);
        Comp<DragonRiftComponent>(carpUid).Dragon = uid;
    }

    // TODO: just make this a move speed modifier component???
    private void OnDragonMove(EntityUid uid, DragonComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.Weakened)
        {
            args.ModifySpeed(0.5f, 0.5f);
        }
    }

    private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
    {
        // Deletes all rifts after dying
        if (args.NewMobState != MobState.Dead)
            return;

        if (component.SoundDeath != null)
            _audio.PlayPvs(component.SoundDeath, uid);

        // objective is explicitly not reset so that it will show how many you got before dying in round end text
        DeleteRifts(uid, false, component);
    }

    private void OnCreated(EntityUid uid, DragonComponent comp, ref GenericAntagCreatedEvent args)
    {
        var mindId = args.MindId;
        var mind = args.Mind;

        _role.MindAddRole(mindId, new DragonRoleComponent(), mind);
        _role.MindAddRole(mindId, new RoleBriefingComponent()
        {
            Briefing = Loc.GetString("dragon-role-briefing")
        }, mind);
    }

    private void Roar(EntityUid uid, DragonComponent comp)
    {
        if (comp.SoundRoar != null)
            _audio.PlayPvs(comp.SoundRoar, uid);
    }

    /// <summary>
    /// Delete all rifts this dragon made.
    /// </summary>
    /// <param name="uid">Entity id of the dragon</param>
    /// <param name="resetRole">If true, the role's rift count will be reset too</param>
    /// <param name="comp">The dragon component</param>
    public void DeleteRifts(EntityUid uid, bool resetRole, DragonComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        foreach (var rift in comp.Rifts)
        {
            QueueDel(rift);
        }

        comp.Rifts.Clear();

        // stop here if not trying to reset the objective's rift count
        if (!resetRole || !TryComp<MindContainerComponent>(uid, out var mindContainer) || !mindContainer.HasMind)
            return;

        var mind = Comp<MindComponent>(mindContainer.Mind.Value);
        foreach (var objId in mind.AllObjectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _carpRifts.ResetRifts(objId, obj);
                break;
            }
        }
    }

    /// <summary>
    /// Increment the dragon role's charged rift count.
    /// </summary>
    public void RiftCharged(EntityUid uid, DragonComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || !mindContainer.HasMind)
            return;

        var mind = Comp<MindComponent>(mindContainer.Mind.Value);
        foreach (var objId in mind.AllObjectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _carpRifts.RiftCharged(objId, obj);
                break;
            }
        }
    }

    /// <summary>
    /// Do everything that needs to happen when a rift gets destroyed by the crew.
    /// </summary>
    public void RiftDestroyed(EntityUid uid, DragonComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        // do reset the rift count since crew destroyed the rift, not deleted by the dragon dying.
        DeleteRifts(uid, true, comp);

        // We can't predict the rift being destroyed anyway so no point adding weakened to shared.
        comp.WeakenedAccumulator = comp.WeakenedDuration;
        _movement.RefreshMovementSpeedModifiers(uid);
        _popup.PopupEntity(Loc.GetString("carp-rift-destroyed"), uid, uid);
    }
}
