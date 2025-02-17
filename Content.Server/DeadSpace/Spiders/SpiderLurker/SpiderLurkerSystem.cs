// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.DeadSpace.Spiders.SpiderLurker;
using Content.Shared.DeadSpace.Spiders.SpiderLurker.Components;
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

namespace Content.Server.DeadSpace.Spiders.SpiderLurker;

public sealed class SpiderLurkerSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderLurkerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpiderLurkerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpiderLurkerComponent, SpiderLurkerActionEvent>(OnHide);
        SubscribeLocalEvent<SpiderLurkerComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<SpiderLurkerComponent, TryingToSleepEvent>(OnSleepAttempt);
    }

    private void OnComponentInit(EntityUid uid, SpiderLurkerComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SpiderLurkerActionEntity, component.SpiderLurker, uid);
    }

    private void OnShutdown(EntityUid uid, SpiderLurkerComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SpiderLurkerActionEntity);
    }

    private void OnRefresh(EntityUid uid, SpiderLurkerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiplier, component.MovementSpeedMultiplier);
    }

    private void OnSleepAttempt(EntityUid uid, SpiderLurkerComponent component, ref TryingToSleepEvent args)
    {
        args.Cancelled = true;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spiderLurkerQuery = EntityQueryEnumerator<SpiderLurkerComponent>();
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
    private void OnHide(EntityUid uid, SpiderLurkerComponent component, SpiderLurkerActionEvent args)
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
            SetReagentCount(uid, -component.BloodCost, bloodsuckerComponent);
        }

        args.Handled = true;

        SetLurker(uid, component, true);

        var xform = Transform(uid);
        var mapCoords = _transform.GetMapCoordinates(uid, xform);
        if (!_mapMan.TryFindGridAt(mapCoords, out _, out var grid) ||
            !grid.TryGetTileRef(xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsSpace())
        {
            return;
        }

        var coords = grid.MapToGrid(mapCoords);
        var ent = Spawn(component.SmokePrototype, coords.SnapToGrid());

        if (!TryComp<SmokeComponent>(ent, out var smoke))
        {
            Log.Error($"Smoke prototype {component.SmokePrototype} was missing SmokeComponent");
            Del(ent);
            return;
        }

        _smokeSystem.StartSmoke(ent, component.Solution, component.Duration, component.SpreadAmount, smoke);
    }

    private void SetLurker(EntityUid uid, SpiderLurkerComponent component, bool isHide)
    {
        if (isHide)
        {
            _appearance.SetData(uid, SpiderLurkerVisuals.state, false);
            _appearance.SetData(uid, SpiderLurkerVisuals.hide, true);

            component.MovementSpeedMultiplier = component.MovementBuff;
            _movement.RefreshMovementSpeedModifiers(uid);

            component.TimeLeftHide = _gameTiming.CurTime + component.DurationHide;
            component.IsHide = true;
        }
        else
        {
            _appearance.SetData(uid, SpiderLurkerVisuals.state, true);
            _appearance.SetData(uid, SpiderLurkerVisuals.hide, false);

            component.MovementSpeedMultiplier = 1f;
            _movement.RefreshMovementSpeedModifiers(uid);
            component.IsHide = false;
        }
    }

}
