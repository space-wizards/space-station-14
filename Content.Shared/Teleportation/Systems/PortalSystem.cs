using System.Linq;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// This handles teleporting entities through portals, and creating new linked portals.
/// </summary>
public sealed class PortalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _linked = default!;

    private const string PortalFixture = "portalFixture";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PortalComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, PortalComponent component, StartCollideEvent args)
    {
        if (args.OurFixture.ID != PortalFixture)
            return;

        var subject = args.OtherFixture.Body.Owner;

        // if they came from another portal, just remove the marker and return
        if (HasComp<PortalTimeoutComponent>(subject))
        {
            RemComp <PortalTimeoutComponent>(subject);
            return;
        }

        if (TryComp<LinkedEntityComponent>(uid, out var link) && link.LinkedEntities.Any())
        {
            // pick a target and teleport there
            var target = _random.Pick(link.LinkedEntities);

            if (HasComp<PortalComponent>(target))
            {
                // if target is a portal, signal that they shouldn't be immediately portaled back
                EnsureComp<PortalTimeoutComponent>(subject);
            }

            TeleportEntity(uid, subject, Transform(target).Coordinates);
        }

        // no linked entity--teleport randomly
        var randVector = _random.NextVector2(component.MaxRandomRadius);
        var newCoords = Transform(uid).Coordinates.Offset(randVector);
        TeleportEntity(uid, subject, newCoords);
    }

    private void TeleportEntity(EntityUid portal, EntityUid subject, EntityCoordinates target)
    {
        // TODO
        // Sound + popup

        Transform(subject).Coordinates = target;
    }

    public void CreateLinkedPortals(EntityCoordinates first, EntityCoordinates second, string firstProto, string secondProto)
    {
        var firstEnt = Spawn(firstProto, first);
        var secondEnt = Spawn(secondProto, second);

        _linked.TryLink(firstEnt, secondEnt);
    }
}
