using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Shuttles.Components;
using Content.Shared.Waypointer;
using Robust.Client.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Client.Waypointer;

/// <summary>
/// This Overlay draws the waypointers on the screen.
/// </summary>
public sealed class WaypointerOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager  _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly ShaderInstance _unshadedShader;
    private readonly SharedPhysicsSystem _physics;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    internal WaypointerOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();
        _physics = _entity.System<SharedPhysicsSystem>();
        _unshadedShader = _prototype.Index(UnshadedShader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_unshadedShader); // Waypointers are unshaded.

        var query = _entity.AllEntityQueryEnumerator<WaypointerComponent, TransformComponent>();
        while (query.MoveNext(out var player, out var comp, out var playerXform))
        {
            // Waypointers are client-side, so we do not draw the waypointers if it's about the client-entity.
            if (playerXform.MapID != args.MapId || player != _playerManager.LocalEntity)
                continue;

            if (!_prototype.Resolve(comp.WaypointerProtoId, out var prototype)
                // Check if the waypointer works on grid.
                || !prototype.WorkOnGrid && playerXform.GridUid != null)
                return;

            var waypointQuery = _entity.AllEntityQueryEnumerator<StationAnchorComponent, TransformComponent>();
            while (waypointQuery.MoveNext(out _, out _, out var targetXform))
            {
                // Check if the stationAnchor is even on the same map.
                if (targetXform.MapID != args.MapId)
                    continue;

                var grid = _transform.GetGrid(targetXform.Coordinates);

                // Check if they're on a grid.
                if (grid == null
                    || !_entity.TryGetComponent<MapGridComponent>(grid, out var map)
                    || !_entity.TryGetComponent<TransformComponent>(grid, out var gridXform))
                    continue;

                _physics.TryGetDistance(player, grid.Value, out var gridDistance, playerXform, gridXform);

                if (gridDistance > prototype.MaxRange)
                    continue;

                // The StationWaypointer has 5 stages and 150 range. With calculations, it'll check if it's either in:
                // 0-29, 30-59, 60-89, 90-119, 120-150 range and use the respective waypointer sprite for it.
                var increments = prototype.MaxRange / prototype.WaypointerStates;
                var waypointerState = Math.Truncate(gridDistance / increments) + 1;
                var stateName = "marker" + waypointerState;

                var rsi = new SpriteSpecifier.Rsi(new ResPath(prototype.RsiPath), stateName);
                var texture = _sprite.Frame0(rsi);

                var positionA = _transform.GetWorldPosition(playerXform);
                var gridData = _transform.GetWorldPositionRotation(gridXform);
                // Adding the centerVector will get the position of the center from the grid.
                var positionB = gridData.WorldPosition + gridData.WorldRotation.RotateVec(map.LocalAABB.Center);

                var dir = positionA - positionB;
                var angle = dir.ToWorldAngle();

                // This is to draw the Waypointer sprites directly ontop of the entity sprite.
                var offset = new Vector2(texture.Height / 2, texture.Width / 2) / EyeManager.PixelsPerMeter;

                handle.DrawTexture(texture, positionA - offset, angle, prototype.Color);
            }
            handle.SetTransform(Matrix3x2.Identity);
        }
    }
}
