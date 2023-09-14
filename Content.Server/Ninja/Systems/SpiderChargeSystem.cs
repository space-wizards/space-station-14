using Content.Server.Explosion.EntitySystems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Sticky.Events;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Prevents planting a spider charge outside of its location and handles greentext.
/// </summary>
public sealed class SpiderChargeSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderChargeComponent, BeforeRangedInteractEvent>(BeforePlant);
        SubscribeLocalEvent<SpiderChargeComponent, EntityStuckEvent>(OnStuck);
        SubscribeLocalEvent<SpiderChargeComponent, TriggerEvent>(OnExplode);
    }

    /// <summary>
    /// Require that the planter is a ninja and the charge is near the target warp point.
    /// </summary>
    private void BeforePlant(EntityUid uid, SpiderChargeComponent comp, BeforeRangedInteractEvent args)
    {
        var user = args.User;

        if (!_mind.TryGetRole<NinjaRoleComponent>(user, out var role))
        {
            _popup.PopupEntity(Loc.GetString("spider-charge-not-ninja"), user, user);
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
            _popup.PopupEntity(Loc.GetString("spider-charge-too-far"), user, user);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Allows greentext to occur after exploding.
    /// </summary>
    private void OnStuck(EntityUid uid, SpiderChargeComponent comp, EntityStuckEvent args)
    {
        comp.Planter = args.User;
    }

    /// <summary>
    /// Handles greentext after exploding.
    /// Assumes it didn't move and the target was destroyed so be nice.
    /// </summary>
    private void OnExplode(EntityUid uid, SpiderChargeComponent comp, TriggerEvent args)
    {
        if (comp.Planter == null || !_mind.TryGetRole<NinjaRoleComponent>(comp.Planter.Value, out var role))
            return;

        // assumes the target was destroyed, that the charge wasn't moved somehow
        role.SpiderChargeDetonated = true;
    }
}
