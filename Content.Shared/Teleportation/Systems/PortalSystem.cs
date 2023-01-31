using System.Linq;
using Content.Shared.Projectiles;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// This handles teleporting entities through portals, and creating new linked portals.
/// </summary>
public sealed class PortalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPullingSystem _pulling = default!;

    private const string PortalFixture = "portalFixture";
    private const string ProjectileFixture = "projectile";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PortalComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<PortalComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<PortalTimeoutComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PortalTimeoutComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, PortalTimeoutComponent component, ref ComponentGetState args)
    {
        args.State = new PortalTimeoutComponentState(component.EnteredPortal);
    }

    private void OnHandleState(EntityUid uid, PortalTimeoutComponent component, ref ComponentHandleState args)
    {
        if (args.Current is PortalTimeoutComponentState state)
            component.EnteredPortal = state.EnteredPortal;
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

        _audio.PlayPredicted(departureSound, portal, subject);
        _audio.PlayPredicted(arrivalSound, subject, subject);
    }
}
