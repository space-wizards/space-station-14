using System.Numerics;
using Content.Client.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CF.Spells;
using Content.Shared.Fluids.Components;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.CF.Spells;

/// <summary>
/// The green target outline includes every creature near the cursor. The click itself often hits the floor.
/// Called from the one allowed <see cref="ActionTargetAttemptEvent"/> listener.
/// </summary>
public sealed class CFSpellTargetingSystem : EntitySystem
{
    private const float OutlineReach = 1f;

    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private SharedActionsSystem? _actions;

    /// <summary>
    /// If this click did not land on a creature, cast at the outlined one instead.
    /// </summary>
    public void TryUseOutlinedCreature(Entity<EntityTargetActionComponent> ent, ref ActionTargetAttemptEvent args)
    {
        if (args.FoundTarget || !TryComp(ent.Owner, out CFSpellComponent? spell))
            return;

        if (spell.ChosenTrick != 0)
        {
            TryUseObject(ent, spell, ref args);
            return;
        }

        if (spell.Delivery is not (CFSpellDelivery.Projectile or CFSpellDelivery.Target))
            return;

        var user = args.User.Owner;
        if (TryResolve(args.Input.EntityUid, user, ent, out var clicked))
        {
            Cast(ent.Owner, clicked, ref args);
            return;
        }

        var map = _transform.ToMapCoordinates(args.Input.Coordinates);
        if (map.MapId == MapId.Nullspace)
            return;

        var best = EntityUid.Invalid;
        var bestDistance = float.MaxValue;
        var bounds = Box2.CenteredAround(map.Position, new Vector2(OutlineReach, OutlineReach) * 2f);
        foreach (var uid in _lookup.GetEntitiesIntersecting(map.MapId, bounds, LookupFlags.Dynamic | LookupFlags.Approximate))
        {
            if (!TryResolve(uid, user, ent, out var mob))
                continue;

            var distance = (_transform.GetWorldPosition(mob) - map.Position).LengthSquared();
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = mob;
        }

        if (!best.IsValid())
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-pick-creature"), user, user);
            return;
        }

        Cast(ent.Owner, best, ref args);
    }

    private bool TryResolve(EntityUid uid, EntityUid user, Entity<EntityTargetActionComponent> action, out EntityUid mob)
    {
        mob = uid;
        while (mob.IsValid())
        {
            if (HasComp<MobStateComponent>(mob) && Actions.ValidateEntityTarget(user, mob, action))
                return true;

            if (!TryComp(mob, out TransformComponent? xform) || !xform.ParentUid.IsValid() || xform.ParentUid == mob)
                break;

            mob = xform.ParentUid;
        }

        mob = EntityUid.Invalid;
        return false;
    }

    private void Cast(EntityUid action, EntityUid target, ref ActionTargetAttemptEvent args)
    {
        args.Handled = true;
        args.FoundTarget = true;
        var request = new RequestPerformActionEvent(GetNetEntity(action), GetNetEntity(target));
        if (TryComp(action, out CFSpellComponent? spell))
            spell.WriteVolley(request);

        RaisePredictiveEvent(request);
    }

    /// <summary>
    /// Food, Lift, and Tidy use the entity under the cursor. They do not search for a creature.
    /// A click on the floor looks at that spot, because a puddle often has no click of its own.
    /// </summary>
    private void TryUseObject(Entity<EntityTargetActionComponent> ent, CFSpellComponent spell, ref ActionTargetAttemptEvent args)
    {
        var user = args.User.Owner;
        if (args.Input.EntityUid is { Valid: true } clicked
            && !HasComp<MapGridComponent>(clicked)
            && !HasComp<MapComponent>(clicked))
        {
            if (Actions.ValidateEntityTarget(user, clicked, ent))
                Cast(ent.Owner, clicked, ref args);
            else
                _popup.PopupEntity(Loc.GetString("cf-spell-pick-object"), user, user);

            return;
        }

        var map = _transform.ToMapCoordinates(args.Input.Coordinates);
        if (map.MapId == MapId.Nullspace)
            return;

        var best = EntityUid.Invalid;
        var bestDistance = float.MaxValue;
        var bounds = Box2.CenteredAround(map.Position, new Vector2(0.5f, 0.5f));
        foreach (var uid in _lookup.GetEntitiesIntersecting(map.MapId, bounds, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate))
        {
            if (uid == user || Transform(uid).Anchored)
                continue;

            if (!Matches(spell.ChosenTrick, uid) || !Actions.ValidateEntityTarget(user, uid, ent))
                continue;

            var distance = (_transform.GetWorldPosition(uid) - map.Position).LengthSquared();
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = uid;
        }

        if (!best.IsValid())
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-pick-object"), user, user);
            return;
        }

        Cast(ent.Owner, best, ref args);
    }

    private bool Matches(byte trick, EntityUid uid)
    {
        return (CFPrestidigitationTrick) trick switch
        {
            CFPrestidigitationTrick.Food => HasComp<EdibleComponent>(uid),
            CFPrestidigitationTrick.Tidy => HasComp<PuddleComponent>(uid),
            CFPrestidigitationTrick.Lift => !HasComp<MobStateComponent>(uid)
                && !HasComp<StackComponent>(uid)
                && HasComp<ItemComponent>(uid),
            _ => false,
        };
    }

    private SharedActionsSystem Actions => _actions ??= EntityManager.System<SharedActionsSystem>();
}
