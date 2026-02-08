using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Client.Shuttles.Systems;
using Content.Client.Station;
using Content.Shared.CombatMode;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Content.Shared.Waypointer;
using Content.Shared.Whitelist;
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
    [Dependency] private readonly IPlayerManager  _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedCombatModeSystem _combatMode = default!;
    private readonly SharedPhysicsSystem _physics;
    private readonly SpriteSystem _sprite;
    private readonly StationSystem _station;
    private readonly TransformSystem _transform;
    private readonly ShaderInstance _unshadedShader;
    private readonly ShuttleSystem _shuttle;
    private readonly EntityWhitelistSystem _whitelist;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    internal WaypointerOverlay()
    {
        IoCManager.InjectDependencies(this);

        _combatMode = _entity.System<SharedCombatModeSystem>();
        _physics = _entity.System<SharedPhysicsSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _station = _entity.System<StationSystem>();
        _transform = _entity.System<TransformSystem>();
        _unshadedShader = _prototype.Index(UnshadedShader).Instance();
        _shuttle = _entity.System<ShuttleSystem>();
        _whitelist = _entity.System<EntityWhitelistSystem>();
    }

    /// <summary>
    /// This will draw the waypointers on top of the player.
    /// </summary>
    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_unshadedShader); // Waypointers are unshaded.

        if (_player.LocalEntity == null
            || !_entity.TryGetComponent<Shared.Waypointer.Components.WaypointerComponent>(_player.LocalEntity, out var waypointer)
            // Check if the Waypointer hashset is null
            || waypointer.WaypointerProtoIds == null
            || !_entity.TryGetComponent<TransformComponent>(_player.LocalEntity, out var playerXform)
            || playerXform.MapID != args.MapId)
            return;

        var player = _player.LocalEntity.Value;

        foreach (var waypointerProtoId in waypointer.WaypointerProtoIds)
        {
            if (!_prototype.Resolve(waypointerProtoId, out var prototype)
                // Check if the waypointer works on grid and combat
                || !prototype.WorkOnGrid && playerXform.GridUid != null
                || !prototype.WorkInCombat && _combatMode.IsInCombatMode(player))
                continue;

            var waypointQuery = _entity.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Check if the target fails/passes the whitelist/blacklist.
                if (!_whitelist.CheckBoth(target, blacklist: prototype.Blacklist, whitelist: prototype.Whitelist)
                    // Check if the target has a hidden IFF.
                    || _shuttle.HasIFFFlag(target, IFFFlags.Hide))
                    continue;

                // We need to check for StationData specifically, because the station entity is in the nullsphere.
                if (_entity.TryGetComponent<StationDataComponent>(target, out var station))
                {
                    // Then we get the largest grid, which is in the actual map.
                    var mainGrid = _station.GetLargestGrid((target, station));
                    if (mainGrid is not null)
                        target = mainGrid.Value;
                }

                // Check if it has the Transform Component
                if (!_entity.TryGetComponent<TransformComponent>(target, out var targetXform)
                    // Check if the target is even on the same map.
                    || targetXform.MapID != args.MapId)
                    continue;

                _physics.TryGetDistance(player, target, out var distance, playerXform, targetXform);

                // For entities without fixtures, the above method returns 0.
                if (distance == 0)
                {
                    // so we need to calculate the distance ourselves.
                    var a = _transform.GetWorldPosition(playerXform);
                    var b = _transform.GetWorldPosition(targetXform);
                    // This feels like a primary school child trying to do rocket science.
                    // But we kinda just see how big number is when we subtract them from each other. It works?
                    var xCoord = Math.Abs(Math.Pow(a.X - b.X, 2));
                    var yCoord = Math.Abs(Math.Pow(a.Y - b.Y, 2));
                    var squaredDistance = (float) (xCoord + yCoord);
                    // Pythagoras the goat. I can't believe my school education was worth for something. It's all triangles.
                    distance = (float) Math.Sqrt(squaredDistance);
                }

                if (distance > prototype.MaxRange)
                    continue;

                // The NTStationWaypointer has 5 stages and 20 range. With calculations, it'll check if it's either in:
                // 0-39, 40-89, 80-119, 120-159, 160-200 range and use the respective waypointer sprite for it.
                var increments = prototype.MaxRange / prototype.WaypointerStates;
                var waypointerState = Math.Truncate(distance / increments) + 1;
                var stateName = "marker" + waypointerState;

                var rsi = new SpriteSpecifier.Rsi(prototype.RsiPath, stateName);
                var texture = _sprite.Frame0(rsi);

                var positionA = _transform.GetWorldPosition(playerXform);
                Vector2 positionB;

                // Check if it's a grid.
                if (_entity.TryGetComponent<MapGridComponent>(target, out var map))
                {
                    var gridData = _transform.GetWorldPositionRotation(targetXform);
                    // Adding the centerVector will get the position of the center from the grid.
                    positionB = gridData.WorldPosition + gridData.WorldRotation.RotateVec(map.LocalAABB.Center);
                }
                else
                {
                    // Else use the current world position.
                    positionB = _transform.GetWorldPosition(targetXform);
                }

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
