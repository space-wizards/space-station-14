using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly PullingSystem _pulling = default!;
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

    private void OnGetVerbs(Entity<PortalComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        // Traversal altverb for ghosts to use that bypasses normal functionality
        if (!args.CanAccess || !HasComp<GhostComponent>(args.User))
            return;

        // Don't use the verb with unlinked or with multi-output portals
        // (this is only intended to be useful for ghosts to see where a linked portal leads)
        var disabled = !TryComp<LinkedEntityComponent>(ent, out var link) || link.LinkedEntities.Count != 1;

        var subject = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 11,
            Act = () =>
            {
                if (link == null || disabled)
                    return;

                var destination = link.LinkedEntities.First();

                // client can't predict outside of simple portal-to-portal interactions due to randomness involved
                // --also can't predict if the target doesn't exist on the client / is outside of PVS
                if (_netMan.IsClient)
                {
                    var exists = Exists(destination);
                    if (!exists || (exists && Transform(destination).MapID == MapId.Nullspace))
                        return;
                }

                TeleportEntity(ent, subject, Transform(destination).Coordinates, destination, false);
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

    private void OnCollide(Entity<PortalComponent> ent, ref StartCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixtureId, args.OtherFixtureId, args.OurFixture, args.OtherFixture))
            return;

        var subject = args.OtherEntity;

        // best not.
        if (Transform(subject).Anchored)
            return;

        // break pulls before portal enter so we dont break shit
        if (TryComp<PullableComponent>(subject, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(subject, pullable);
        }

        if (TryComp<PullerComponent>(subject, out var pullerComp)
            && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
        {
            _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling);
        }

        // if they came from another portal, just return and wait for them to exit the portal
        if (HasComp<PortalTimeoutComponent>(subject))
        {
            return;
        }

        if (TryComp<LinkedEntityComponent>(ent, out var link))
        {
            if (link.LinkedEntities.Count == 0)
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
                timeout.EnteredPortal = ent;
                Dirty(subject, timeout);
            }

            TeleportEntity(ent, subject, Transform(target).Coordinates, target);
            return;
        }

        if (_netMan.IsClient)
            return;

        // no linked entity--teleport randomly
        if (ent.Comp.RandomTeleport)
            TeleportRandomly(ent, subject);
    }

    private void OnEndCollide(Entity<PortalComponent> ent, ref EndCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixtureId, args.OtherFixtureId, args.OurFixture, args.OtherFixture))
            return;

        var subject = args.OtherEntity;

        // if they came from (not us), remove the timeout
        if (TryComp<PortalTimeoutComponent>(subject, out var timeout) && timeout.EnteredPortal != ent)
        {
            RemCompDeferred<PortalTimeoutComponent>(subject);
        }
    }

    private void TeleportEntity(Entity<PortalComponent> ent, EntityUid subject, EntityCoordinates target, EntityUid? targetEntity = null, bool playSound = true)
    {
        var ourCoords = Transform(ent).Coordinates;
        var onSameMap = _transform.GetMapId(ourCoords) == _transform.GetMapId(target);
        var distanceInvalid = ent.Comp.MaxTeleportRadius != null
                              && ourCoords.TryDistance(EntityManager, target, out var distance)
                              && distance > ent.Comp.MaxTeleportRadius;

        if (!onSameMap && !ent.Comp.CanTeleportToOtherMaps || distanceInvalid)
        {
            if (!_netMan.IsServer)
                return;

            // Early out if this is an invalid configuration
            _popup.PopupCoordinates(Loc.GetString("portal-component-invalid-configuration-fizzle"),
                ourCoords, Filter.Pvs(ourCoords, entityMan: EntityManager), true);

            _popup.PopupCoordinates(Loc.GetString("portal-component-invalid-configuration-fizzle"),
                target, Filter.Pvs(target, entityMan: EntityManager), true);

            QueueDel(ent);

            if (targetEntity != null)
                QueueDel(targetEntity.Value);

            return;
        }

        var arrivalSound = CompOrNull<PortalComponent>(targetEntity)?.ArrivalSound ?? ent.Comp.ArrivalSound;
        var departureSound = ent.Comp.DepartureSound;

        // Some special cased stuff: projectiles should stop ignoring shooter when they enter a portal, to avoid
        // stacking 500 bullets in between 2 portals and instakilling people--you'll just hit yourself instead
        // (as expected)
        if (TryComp<ProjectileComponent>(subject, out var projectile))
        {
            projectile.IgnoreShooter = false;
        }

        LogTeleport(ent, subject, Transform(subject).Coordinates, target);

        _transform.SetCoordinates(subject, target);

        if (!playSound)
            return;

        _audio.PlayPredicted(departureSound, ent, subject);
        _audio.PlayPredicted(arrivalSound, subject, subject);
    }

    private void TeleportRandomly(Entity<PortalComponent> ent, EntityUid subject)
    {
        var xform = Transform(ent);
        var coords = xform.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(ent.Comp.MaxRandomRadius));
        for (var i = 0; i < MaxRandomTeleportAttempts; i++)
        {
            var randVector = _random.NextVector2(ent.Comp.MaxRandomRadius);
            newCoords = coords.Offset(randVector);
            if (!_lookup.AnyEntitiesIntersecting(_transform.ToMapCoordinates(newCoords), LookupFlags.Static))
            {
                break;
            }
        }

        TeleportEntity(ent, subject, newCoords);
    }

    protected virtual void LogTeleport(EntityUid portal, EntityUid subject, EntityCoordinates source,
        EntityCoordinates target)
    {
    }
}
