using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// This handles teleporting entities through portals, and creating new linked portals.
/// </summary>
public abstract class SharedPortalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const string PortalFixture = "portalFixture";
    private const string ProjectileFixture = "projectile";

    private const int MaxRandomTeleportAttempts = 20;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PortalComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<PortalComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<PortalComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, PortalComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        // Traversal altverb for ghosts to use that bypasses normal functionality
        if (!args.CanAccess || !HasComp<GhostComponent>(args.User))
            return;

        // Don't use the verb with unlinked or with multi-output portals
        // (this is only intended to be useful for ghosts to see where a linked portal leads)
        var disabled = !TryComp<LinkedEntityComponent>(uid, out var link) || link.LinkedEntities.Count != 1;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 11,
            Act = () =>
            {
                if (link == null || disabled)
                    return;

                var ent = link.LinkedEntities.First();
                TeleportEntity(uid, args.User, Transform(ent).Coordinates, ent, false);
            },
            Disabled = disabled,
            Text = Loc.GetString("portal-component-ghost-traverse"),
            Message = disabled
                ? Loc.GetString("portal-component-no-linked-entities")
                : Loc.GetString("portal-component-can-ghost-traverse"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"))
        });
    }

    private bool ShouldCollide(string ourId, string otherId, Fixture our, Fixture other)
    {
        // most non-hard fixtures shouldn't pass through portals, but projectiles are non-hard as well
        // and they should still pass through
        return ourId == PortalFixture && (other.Hard || otherId == ProjectileFixture);
    }

    private void OnCollide(EntityUid uid, PortalComponent component, ref StartCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixtureId, args.OtherFixtureId, args.OurFixture, args.OtherFixture))
            return;

        var subject = args.OtherEntity;

        // best not.
        if (Transform(subject).Anchored)
            return;

        // break pulls before portal enter so we dont break shit
        if (TryComp<SharedPullableComponent>(subject, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(pullable);
        }

        if (TryComp<SharedPullerComponent>(subject, out var pulling)
            && pulling.Pulling != null && TryComp<SharedPullableComponent>(pulling.Pulling.Value, out var subjectPulling))
        {
            _pulling.TryStopPull(subjectPulling);
        }

        // if they came from another portal, just return and wait for them to exit the portal
        if (HasComp<PortalTimeoutComponent>(subject))
        {
            return;
        }

        if (TryComp<LinkedEntityComponent>(uid, out var link))
        {
            if (!link.LinkedEntities.Any())
                return;

            // client can't predict outside of simple portal-to-portal interactions due to randomness involved
            // --also can't predict if the target doesn't exist on the client / is outside of PVS
            if (_netMan.IsClient)
            {
                var first = link.LinkedEntities.First();
                var exists = Exists(first);
                if (link.LinkedEntities.Count != 1 || !exists || (exists && Transform(first).MapID == MapId.Nullspace))
                    return;
            }

            // pick a target and teleport there
            var target = _random.Pick(link.LinkedEntities);

            if (HasComp<PortalComponent>(target))
            {
                // if target is a portal, signal that they shouldn't be immediately portaled back
                var timeout = EnsureComp<PortalTimeoutComponent>(subject);
                timeout.EnteredPortal = uid;
                Dirty(timeout);
            }

            TeleportEntity(uid, subject, Transform(target).Coordinates, target);
            return;
        }

        if (_netMan.IsClient)
            return;

        // no linked entity--teleport randomly
        TeleportRandomly(uid, subject, component);
    }

    private void OnEndCollide(EntityUid uid, PortalComponent component, ref EndCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixtureId, args.OtherFixtureId,args.OurFixture, args.OtherFixture))
            return;

        var subject = args.OtherEntity;

        // if they came from (not us), remove the timeout
        if (TryComp<PortalTimeoutComponent>(subject, out var timeout) && timeout.EnteredPortal != uid)
        {
            RemCompDeferred<PortalTimeoutComponent>(subject);
        }
    }

    private void TeleportEntity(EntityUid portal, EntityUid subject, EntityCoordinates target, EntityUid? targetEntity=null, bool playSound=true,
        PortalComponent? portalComponent = null)
    {
        if (!Resolve(portal, ref portalComponent))
            return;

        var ourCoords = Transform(portal).Coordinates;
        var onSameMap = ourCoords.GetMapId(EntityManager) == target.GetMapId(EntityManager);
        var distanceInvalid = portalComponent.MaxTeleportRadius != null
                              && ourCoords.TryDistance(EntityManager, target, out var distance)
                              && distance > portalComponent.MaxTeleportRadius;

        if (!onSameMap && !portalComponent.CanTeleportToOtherMaps || distanceInvalid)
        {
            if (!_netMan.IsServer)
                return;

            // Early out if this is an invalid configuration
            _popup.PopupCoordinates(Loc.GetString("portal-component-invalid-configuration-fizzle"),
                ourCoords, Filter.Pvs(ourCoords, entityMan: EntityManager), true);

            _popup.PopupCoordinates(Loc.GetString("portal-component-invalid-configuration-fizzle"),
                target, Filter.Pvs(target, entityMan: EntityManager), true);

            QueueDel(portal);

            if (targetEntity != null)
                QueueDel(targetEntity.Value);

            return;
        }

        var arrivalSound = CompOrNull<PortalComponent>(targetEntity)?.ArrivalSound ?? portalComponent.ArrivalSound;
        var departureSound = portalComponent.DepartureSound;

        // Some special cased stuff: projectiles should stop ignoring shooter when they enter a portal, to avoid
        // stacking 500 bullets in between 2 portals and instakilling people--you'll just hit yourself instead
        // (as expected)
        if (TryComp<ProjectileComponent>(subject, out var projectile))
        {
            projectile.IgnoreShooter = false;
        }

        LogTeleport(portal, subject, Transform(subject).Coordinates, target);

        _transform.SetCoordinates(subject, target);

        if (!playSound)
            return;

        _audio.PlayPredicted(departureSound, portal, subject);
        _audio.PlayPredicted(arrivalSound, subject, subject);
    }

    private void TeleportRandomly(EntityUid portal, EntityUid subject, PortalComponent? component = null)
    {
        if (!Resolve(portal, ref component))
            return;

        var xform = Transform(portal);
        var coords = xform.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(component.MaxRandomRadius));
        for (var i = 0; i < MaxRandomTeleportAttempts; i++)
        {
            var randVector = _random.NextVector2(component.MaxRandomRadius);
            newCoords = coords.Offset(randVector);
            if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transform), LookupFlags.Static).Any())
            {
                break;
            }
        }

        TeleportEntity(portal, subject, newCoords);
    }

    protected virtual void LogTeleport(EntityUid portal, EntityUid subject, EntityCoordinates source,
        EntityCoordinates target)
    {
    }
}
