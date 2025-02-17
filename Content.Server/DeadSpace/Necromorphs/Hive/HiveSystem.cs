// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Popups;
using Content.Shared.RatKing;
using Content.Server.RatKing;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Shared.Timing;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.DeadSpace.Necromorphs.Hive;

public sealed class HiveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HiveComponent, RatKingRaiseArmyActionEvent>(OnRaiseArmy, before: new[] { typeof(RatKingSystem) });
        SubscribeLocalEvent<HiveComponent, RatKingDomainActionEvent>(OnDomain, before: new[] { typeof(RatKingSystem) });
    }
    private void OnComponentInit(EntityUid uid, HiveComponent component, ComponentInit args)
    {
        component.NextTick = _timing.CurTime + component.Duration;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HiveComponent, LimitedChargesComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (_timing.CurTime > comp.NextTick)
            {
                _charges.AddCharges(uid, 1, xform);
                comp.NextTick = _timing.CurTime + comp.Duration;
                _popup.PopupEntity(Loc.GetString($"Способность восстановилась, количество = {xform.Charges}"), uid, uid);
            }
        }
    }
    private void OnDomain(EntityUid uid, HiveComponent component, RatKingDomainActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<RatKingComponent>(uid, out var ratKing))
        {
            var tileMix = _atmos.GetTileMixture(uid, excite: true);
            tileMix?.AdjustMoles(Gas.InfectionDeadSpace, ratKing.MolesAmmoniaPerDomain);
        }
    }
    private void OnRaiseArmy(EntityUid uid, HiveComponent component, RatKingRaiseArmyActionEvent args)
    {
        if (args.Handled)
            return;

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете применить способность"), uid, uid);
            args.Handled = true;
            return;
        }

        if (charges != null)
            _charges.UseCharge(uid, charges);
    }
}
