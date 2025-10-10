using Content.Shared.Maths;
using Content.Shared.Teleportation.Components;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.Warps;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// <inheritdoc cref="TeleportLocationsComponent"/>
/// </summary>
public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected const string TeleportDelay = "TeleportDelay";
    protected const string TeleportFailedDelay = "TeleportFailedDelay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationDestinationMessage>(OnTeleportToLocationRequest);
    }

    protected bool IsDelayed(EntityUid entityUid)
    {
        return _delay.IsDelayed(entityUid, TeleportDelay) || _delay.IsDelayed(entityUid, TeleportFailedDelay);
    }

    private void OnUiOpenAttempt(Entity<TeleportLocationsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!IsDelayed(ent))
            return;

        args.Cancel();
    }

    protected virtual void OnTeleportToLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (!TryGetEntity(args.NetEnt, out var telePointEnt) || TerminatingOrDeleted(telePointEnt) || !HasComp<WarpPointComponent>(telePointEnt) || IsDelayed(ent))
            return;

        var comp = ent.Comp;
        var originEnt = args.Actor;
        var telePointXForm = Transform(telePointEnt.Value);

        // Spawn effect even if the target is unsafe - the failure is funny.
        var originEntXForm = Transform(originEnt);
        SpawnAtPosition(comp.TeleportEffect, originEntXForm.Coordinates);

        if (ChooseSafeLocation((telePointEnt.Value, telePointXForm), maxDistance: 3) is not { } safeTargetMapCoords)
        {
            // Prevent spamming effects if the target is obstructed.
            _delay.TryResetDelay(ent.Owner, true, id: TeleportFailedDelay);

            return;
        }

        _xform.SetMapCoordinates(originEnt, safeTargetMapCoords);
        SpawnAtPosition(comp.TeleportEffect, originEntXForm.Coordinates);

        _delay.TryResetDelay(ent.Owner, true, id: TeleportDelay);

        if (!ent.Comp.CloseAfterTeleport)
            return;

        // Teleport's done, now tell the BUI to close if needed.
        _ui.CloseUi(ent.Owner, TeleportLocationUiKey.Key);
    }

    /// <remarks>
    /// The result of this overload might be different between the client and server due to PVS.
    /// </remarks>
    private MapCoordinates? ChooseSafeLocation(Entity<TransformComponent> targetEntity, int maxDistance)
    {
        // If the target point is on a grid, use that grid's rotation.
        var gridTransform = targetEntity.Comp.GridUid is { } grid
            ? Matrix3Helpers.CreateTransform(Vector2.Zero, _xform.GetWorldRotation(Transform(grid)))
            : Matrix3x2.Identity;

        var targetCoords = _xform.GetMapCoordinates(targetEntity);
        // The entity might've left PVS on the client.
        if (targetCoords.MapId == MapId.Nullspace)
            return null;

        return ChooseSafeLocation(targetCoords, maxDistance, gridTransform);
    }

    private MapCoordinates? ChooseSafeLocation(MapCoordinates targetCoords, int maxDistance, Matrix3x2 gridTransform)
    {
        var maxAttempts = UlamSpiral.PointsForMaxDistance(maxDistance);
        // Transforms an offset from the target entity into the final world space position.
        var worldToGridSpacePlusTargetPos = gridTransform * Matrix3x2.CreateTranslation(targetCoords.Position);

        for (var attempt = 0; attempt <= maxAttempts; attempt++)
        {
            var offset = UlamSpiral.Point(attempt);
            var offsetWorldPos = Vector2.Transform(new Vector2(offset.X, offset.Y), worldToGridSpacePlusTargetPos);
            var offsetCoords = new MapCoordinates(offsetWorldPos, targetCoords.MapId);

            if (!_lookup.AnyEntitiesIntersecting(offsetCoords, LookupFlags.Static))
            {
                // Selected location is not inside a wall.
                return offsetCoords;
            }
        }

        return null;
    }
}
