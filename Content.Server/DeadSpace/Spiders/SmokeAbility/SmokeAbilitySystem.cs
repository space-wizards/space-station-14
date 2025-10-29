// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.DeadSpace.Spiders.SmokeAbility;
using Content.Shared.DeadSpace.Spiders.SmokeAbility.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Timing;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;
using Content.Server.Popups;
using Content.Shared.Bed.Sleep;
using Content.Server.Spreader;

namespace Content.Server.DeadSpace.Spiders.SmokeAbility;

public sealed class SmokeAbilitySystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SmokeAbilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SmokeAbilityComponent, SmokeAbilityActionEvent>(OnHide);
        SubscribeLocalEvent<SmokeAbilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<SmokeAbilityComponent, TryingToSleepEvent>(OnSleepAttempt);
    }

    private void OnComponentInit(EntityUid uid, SmokeAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SmokeAbilityActionEntity, component.SmokeAbility, uid);
    }

    private void OnShutdown(EntityUid uid, SmokeAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SmokeAbilityActionEntity);
    }

    private void OnRefresh(EntityUid uid, SmokeAbilityComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiplier, component.MovementSpeedMultiplier);
    }

    private void OnSleepAttempt(EntityUid uid, SmokeAbilityComponent component, ref TryingToSleepEvent args)
    {
        args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spiderLurkerQuery = EntityQueryEnumerator<SmokeAbilityComponent>();
        while (spiderLurkerQuery.MoveNext(out var ent, out var spiderLurker))
        {
            if (spiderLurker.IsHide)
            {
                if (_gameTiming.CurTime > spiderLurker.TimeLeftHide)
                {
                    SetLurker(ent, spiderLurker, false);
                }
            }
        }
    }

    private void OnHide(EntityUid uid, SmokeAbilityComponent component, SmokeAbilityActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<BloodsuckerComponent>(uid, out var bloodsuckerComponent))
        {
            if (bloodsuckerComponent.CountReagent < component.BloodCost)
            {
                var countReagent = bloodsuckerComponent.CountReagent;
                _popup.PopupEntity(Loc.GetString("Недостаточно питательных веществ, у вас ") + countReagent.ToString() + Loc.GetString(" а нужно: ") + component.BloodCost.ToString(), uid, uid);
                return;
            }
            AddReagentCount(uid, -component.BloodCost, bloodsuckerComponent);
        }

        args.Handled = true;

        SetLurker(uid, component, true);

        var xform = Transform(uid);
        var mapCoords = _transform.GetMapCoordinates(uid, xform);
        if (!_mapMan.TryFindGridAt(mapCoords, out var gridUid, out var grid) ||
            !_map.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsEmpty)
        {
            return;
        }

        if (_spreader.RequiresFloorToSpread(component.SmokePrototype.ToString()) && _turf.IsSpace(tileRef))
            return;

        var coords = _map.MapToGrid(gridUid, mapCoords);
        var ent = Spawn(component.SmokePrototype, coords.SnapToGrid());
        if (!TryComp<SmokeComponent>(ent, out var smoke))
        {
            Log.Error($"Smoke prototype {component.SmokePrototype} was missing SmokeComponent");
            Del(ent);
            return;
        }

        _smokeSystem.StartSmoke(ent, component.Solution, component.Duration, component.SpreadAmount, smoke);
    }

    private void SetLurker(EntityUid uid, SmokeAbilityComponent component, bool isHide)
    {
        if (!component.ChangeApperacne)
            return;

        if (isHide)
        {
            _appearance.SetData(uid, SmokeAbilityVisuals.state, false);
            _appearance.SetData(uid, SmokeAbilityVisuals.hide, true);

            component.MovementSpeedMultiplier = component.MovementBuff;
            _movement.RefreshMovementSpeedModifiers(uid);

            component.TimeLeftHide = _gameTiming.CurTime + component.DurationHide;
            component.IsHide = true;
        }
        else
        {
            _appearance.SetData(uid, SmokeAbilityVisuals.state, true);
            _appearance.SetData(uid, SmokeAbilityVisuals.hide, false);

            component.MovementSpeedMultiplier = 1f;
            _movement.RefreshMovementSpeedModifiers(uid);
            component.IsHide = false;
        }
    }
}
