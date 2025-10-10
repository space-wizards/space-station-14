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
    [Dependency] protected readonly UseDelaySystem Delay = default!;

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected const string TeleportDelay = "TeleportDelay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationDestinationMessage>(OnTeleportToLocationRequest);
    }

    private void OnUiOpenAttempt(Entity<TeleportLocationsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        args.Cancel();
    }

    protected virtual void OnTeleportToLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (!TryGetEntity(args.NetEnt, out var telePointEnt) || TerminatingOrDeleted(telePointEnt) || !HasComp<WarpPointComponent>(telePointEnt) || Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        var comp = ent.Comp;
        var originEnt = args.Actor;
        var telePointXForm = Transform(telePointEnt.Value);

        if (ChooseSafeLocation((telePointEnt.Value, telePointXForm), maxDistance: 3) is not { } safeTargetMapCoords)
            return;

        var originEntXForm = Transform(originEnt);

        SpawnAtPosition(comp.TeleportEffect, originEntXForm.Coordinates);

        _xform.SetMapCoordinates(originEnt, safeTargetMapCoords);
        SpawnAtPosition(comp.TeleportEffect, originEntXForm.Coordinates);

        Delay.TryResetDelay(ent.Owner, true, id: TeleportDelay);

        if (!ent.Comp.CloseAfterTeleport)
            return;

        // Teleport's done, now tell the BUI to close if needed.
        _ui.CloseUi(ent.Owner, TeleportLocationUiKey.Key);
    }

    private MapCoordinates? ChooseSafeLocation(Entity<TransformComponent> targetEntity, int maxDistance)
    {
        // If the target point is on a grid, use that grid's rotation.
        var gridTransform = targetEntity.Comp.GridUid is { } grid
            ? Matrix3Helpers.CreateTransform(Vector2.Zero, _xform.GetWorldRotation(Transform(grid)))
            : Matrix3x2.Identity;
        return ChooseSafeLocation(_xform.GetMapCoordinates(targetEntity), maxDistance, gridTransform);
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
