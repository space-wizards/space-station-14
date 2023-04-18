using Content.Server.Explosion.EntitySystems;
using Content.Server.Sticky.Events;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Ninja.Systems;

public sealed class SpiderChargeSystem : EntitySystem
{
    [Dependency] private readonly NinjaSystem _ninja = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderChargeComponent, BeforeRangedInteractEvent>(BeforePlant);
        SubscribeLocalEvent<SpiderChargeComponent, EntityStuckEvent>(OnStuck);
        SubscribeLocalEvent<SpiderChargeComponent, TriggerEvent>(OnExplode);
    }

    private void BeforePlant(EntityUid uid, SpiderChargeComponent comp, BeforeRangedInteractEvent args)
    {
        var user = args.User;

        if (!_ninja.GetNinjaRole(user, out var role))
        {
            _popups.PopupEntity(Loc.GetString("spider-charge-not-ninja"), user, user);
            args.Handled = true;
            return;
        }

        // allow planting anywhere if there is no target, which should never happen
        if (role.SpiderChargeTarget == null)
            return;

        // assumes warp point still exists
        var target = Transform(role.SpiderChargeTarget.Value).MapPosition;
        var coords = args.ClickLocation.ToMap(EntityManager, _transform);
        if (!coords.InRange(target, comp.Range))
        {
            _popups.PopupEntity(Loc.GetString("spider-charge-too-far"), user, user);
            args.Handled = true;
        }
    }

    private void OnStuck(EntityUid uid, SpiderChargeComponent comp, EntityStuckEvent args)
    {
        comp.Planter = args.User;
    }

    private void OnExplode(EntityUid uid, SpiderChargeComponent comp, TriggerEvent args)
    {
        if (comp.Planter == null || !_ninja.GetNinjaRole(comp.Planter.Value, out var role))
            return;

        // assumes the target was destroyed, that the charge wasn't moved somehow
        role.SpiderChargeDetonated = true;
    }
}
