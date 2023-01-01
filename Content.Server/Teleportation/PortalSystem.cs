using System.Linq;
using Content.Shared.Projectiles;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Teleportation;

/// <summary>
/// This handles teleporting entities through portals, and creating new linked portals.
/// </summary>
public sealed class PortalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _linked = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private const string PortalFixture = "portalFixture";
    private const string ProjectileFixture = "projectile";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PortalComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<PortalComponent, EndCollideEvent>(OnEndCollide);
    }

    private bool ShouldCollide(Fixture our, Fixture other)
    {
        // most non-hard fixtures shouldn't pass through portals, but projectiles are non-hard as well
        // and they should still pass through
        return our.ID == PortalFixture && (other.Hard || other.ID == ProjectileFixture);
    }

    private void OnCollide(EntityUid uid, PortalComponent component, ref StartCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixture, args.OtherFixture))
            return;

        var subject = args.OtherFixture.Body.Owner;

        // if they came from another portal, just return and wait for them to exit the portal
        if (HasComp<PortalTimeoutComponent>(subject))
        {
            return;
        }

        if (TryComp<LinkedEntityComponent>(uid, out var link) && link.LinkedEntities.Any())
        {
            // pick a target and teleport there
            var target = _random.Pick(link.LinkedEntities);

            if (HasComp<PortalComponent>(target))
            {
                // if target is a portal, signal that they shouldn't be immediately portaled back
                var timeout = EnsureComp<PortalTimeoutComponent>(subject);
                timeout.EnteredPortal = uid;
            }

            TeleportEntity(uid, subject, Transform(target).Coordinates, target);
            return;
        }

        // no linked entity--teleport randomly
        var randVector = _random.NextVector2(component.MaxRandomRadius);
        var newCoords = Transform(uid).Coordinates.Offset(randVector);
        TeleportEntity(uid, subject, newCoords);
    }

    private void OnEndCollide(EntityUid uid, PortalComponent component, ref EndCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixture, args.OtherFixture))
            return;

        var subject = args.OtherFixture.Body.Owner;

        // if they came from (not us), remove the timeout
        if (TryComp<PortalTimeoutComponent>(subject, out var timeout) && timeout.EnteredPortal != uid)
        {
            RemComp<PortalTimeoutComponent>(subject);
        }
    }

    private void TeleportEntity(EntityUid portal, EntityUid subject, EntityCoordinates target, EntityUid? targetEntity=null,
        PortalComponent? portalComponent = null)
    {
        if (!Resolve(portal, ref portalComponent))
            return;

        var arrivalSound = CompOrNull<PortalComponent>(targetEntity)?.ArrivalSound ?? portalComponent.ArrivalSound;
        var departureSound = portalComponent.DepartureSound;

        // Some special cased stuff: projectiles should stop ignoring shooter when they enter a portal, to avoid
        // stacking 500 bullets in between 2 portals and instakilling people--you'll just hit yourself instead
        // (as expected)
        if (TryComp<ProjectileComponent>(subject, out var projectile))
        {
            projectile.IgnoreShooter = false;
        }

        Transform(subject).Coordinates = target;

        _audio.PlayPvs(departureSound, portal);
        _audio.Play(arrivalSound, Filter.Pvs(target), target, true);
    }

    public void CreateLinkedPortals(EntityCoordinates first, EntityCoordinates second, string firstProto, string secondProto)
    {
        var firstEnt = Spawn(firstProto, first);
        var secondEnt = Spawn(secondProto, second);

        _linked.TryLink(firstEnt, secondEnt);
    }
}
