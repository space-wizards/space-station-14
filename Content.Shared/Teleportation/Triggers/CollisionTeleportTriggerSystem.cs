using Content.Shared.Teleportation.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Teleportation.Triggers;

public sealed partial class CollisionTeleportTriggerSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollisionTeleportTriggerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CollisionTeleportTriggerComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(Entity<CollisionTeleportTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!IsTriggerCollision(ent.Comp, args.OurFixtureId, args.OtherFixtureId, args.OtherFixture))
            return;

        var target = args.OtherEntity;

        if (!IsTargetAllowed(ent.Comp, target))
            return;

        _teleport.RequestTeleport(ent, target, target);
    }

    private void OnEndCollide(Entity<CollisionTeleportTriggerComponent> ent, ref EndCollideEvent args)
    {
        if (!IsTriggerCollision(ent.Comp, args.OurFixtureId, args.OtherFixtureId, args.OtherFixture))
            return;

        var exited = new TeleportTriggerExitedEvent(args.OtherEntity);
        RaiseLocalEvent(ent, ref exited);
    }

    /// <summary>
    /// Whether the target's fixtures and filters allow it to activate this trigger after teleporting here.
    /// Does not check the teleport destination or current overlap.
    /// </summary>
    public bool CanTrigger(Entity<CollisionTeleportTriggerComponent?> teleporter, EntityUid target)
    {
        if (!Resolve(teleporter, ref teleporter.Comp, logMissing: false))
            return false;

        if (!IsTargetAllowed(teleporter.Comp, target))
            return false;

        if (!TryComp<PhysicsComponent>(teleporter, out var teleporterBody))
            return false;

        if (!teleporterBody.CanCollide)
            return false;

        if (!TryComp<PhysicsComponent>(target, out var targetBody))
            return false;

        if (!targetBody.CanCollide)
            return false;

        if (!TryComp<FixturesComponent>(teleporter, out var teleporterFixtures))
            return false;

        if (!TryComp<FixturesComponent>(target, out var targetFixtures))
            return false;

        foreach (var (teleporterId, teleporterFixture) in teleporterFixtures.Fixtures)
        {
            if (!IsTriggerFixture(teleporter.Comp, teleporterId))
                continue;

            if (CanTriggerFixture(teleporter.Comp, teleporterFixture, targetFixtures))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the target overlaps the conservative world bounds of this trigger's fixtures.
    /// Uses current geometry rather than physics contacts or collision eligibility, so temporarily
    /// disabling collisions does not release a target that is still inside the exit.
    /// Bounds can retain the block near the corners of rotated or non-rectangular shapes.
    /// </summary>
    public bool IsInsideTriggerBounds(Entity<CollisionTeleportTriggerComponent?> teleporter, EntityUid target)
    {
        if (!Resolve(teleporter, ref teleporter.Comp, logMissing: false))
            return false;

        if (!Exists(teleporter))
            return false;

        if (TerminatingOrDeleted(teleporter))
            return false;

        if (!Exists(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!TryComp<FixturesComponent>(teleporter, out var teleporterFixtures))
            return false;

        if (!TryComp<FixturesComponent>(target, out var targetFixtures))
            return false;

        if (!TryComp(teleporter, out TransformComponent? teleporterXform))
            return false;

        if (!TryComp(target, out TransformComponent? targetXform))
            return false;

        if (teleporterXform.MapID == MapId.Nullspace)
            return false;

        if (teleporterXform.MapID != targetXform.MapID)
            return false;

        var teleporterTransform = _physics.GetPhysicsTransform(teleporter, teleporterXform);
        var targetTransform = _physics.GetPhysicsTransform(target, targetXform);

        foreach (var (teleporterId, teleporterFixture) in teleporterFixtures.Fixtures)
        {
            if (!IsTriggerFixture(teleporter.Comp, teleporterId))
                continue;

            foreach (var (targetId, targetFixture) in targetFixtures.Fixtures)
            {
                if (!IsTargetFixtureAllowed(teleporter.Comp, targetId, targetFixture))
                    continue;

                if (FixtureBoundsOverlap(teleporterFixture, targetFixture, teleporterTransform, targetTransform))
                    return true;
            }
        }

        return false;
    }

    private static bool FixtureBoundsOverlap(
        Fixture teleporterFixture,
        Fixture targetFixture,
        Transform teleporterTransform,
        Transform targetTransform)
    {
        for (var i = 0; i < teleporterFixture.Shape.ChildCount; i++)
        {
            var teleporterBounds = teleporterFixture.Shape.ComputeAABB(teleporterTransform, i);
            for (var j = 0; j < targetFixture.Shape.ChildCount; j++)
            {
                if (teleporterBounds.Intersects(targetFixture.Shape.ComputeAABB(targetTransform, j)))
                    return true;
            }
        }

        return false;
    }

    private static bool CanTriggerFixture(
        CollisionTeleportTriggerComponent component,
        Fixture teleporterFixture,
        FixturesComponent targetFixtures)
    {
        foreach (var (targetId, targetFixture) in targetFixtures.Fixtures)
        {
            if (!IsTargetFixtureAllowed(component, targetId, targetFixture))
                continue;

            if ((teleporterFixture.CollisionMask & targetFixture.CollisionLayer) != 0 ||
                (targetFixture.CollisionMask & teleporterFixture.CollisionLayer) != 0)
                return true;
        }

        return false;
    }

    private bool IsTargetAllowed(CollisionTeleportTriggerComponent component, EntityUid target)
    {
        if (Transform(target).Anchored)
            return false;

        if (_whitelist.IsWhitelistFail(component.TargetWhitelist, target))
            return false;

        return !_whitelist.IsWhitelistPass(component.TargetBlacklist, target);
    }

    private static bool IsTriggerCollision(
        CollisionTeleportTriggerComponent component,
        string teleporterFixtureId,
        string targetFixtureId,
        Fixture targetFixture)
    {
        return IsTriggerFixture(component, teleporterFixtureId) &&
               IsTargetFixtureAllowed(component, targetFixtureId, targetFixture);
    }

    private static bool IsTriggerFixture(CollisionTeleportTriggerComponent component, string fixtureId)
    {
        return component.TriggerFixtureId == null || component.TriggerFixtureId == fixtureId;
    }

    private static bool IsTargetFixtureAllowed(CollisionTeleportTriggerComponent component, string fixtureId, Fixture fixture)
    {
        return fixture.Hard || component.AllowedNonHardTargetFixtureIds.Contains(fixtureId);
    }
}
