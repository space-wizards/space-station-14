using System.Linq;
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
using Content.Shared.Waypointer.Components;
using Content.Shared.Whitelist;
using Robust.Client.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Client.Waypointer;

/// <summary>
/// This Overlay draws the waypointers on the screen.
/// </summary>
public sealed partial class WaypointerOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager  _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly SharedCombatModeSystem _combatMode;
    private readonly SharedPhysicsSystem _physics;
    private readonly SpriteSystem _sprite;
    private readonly StationSystem _station;
    private readonly TransformSystem _transform;
    private readonly ShuttleSystem _shuttle;
    private readonly EntityWhitelistSystem _whitelist;

    private readonly ShaderInstance _unshadedShader;

    // Caching the Uid for the station grid.
    private EntityUid? _mainStationGrid;

    /// <summary>
    /// The last locations of the tracked entity sent by the server.
    /// The client will disregard them if they can see the entity in their PVS range.
    /// </summary>
    public Dictionary<ProtoId<WaypointerPrototype>, List<(NetEntity, Vector2)>> TrackedServerCoordinates = new ();

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
        if (_mainStationGrid == null)
            _mainStationGrid = GetStationGrid();

        var handle = args.WorldHandle;
        handle.UseShader(_unshadedShader); // Waypointers are unshaded.

        if (_player.LocalEntity == null
            || !_entity.TryGetComponent<ActiveWaypointerComponent>(_player.LocalEntity, out var waypointer)
            || waypointer.WaypointerProtoIds == null
            || !_entity.TryGetComponent<TransformComponent>(_player.LocalEntity, out var playerXform)
            || playerXform.MapID != args.MapId)
            return;

        var player = _player.LocalEntity.Value;
        var playerPosition = _transform.GetWorldPosition(playerXform);

        foreach (var waypointerPair in waypointer.WaypointerProtoIds)
        {
            // The boolean in the dictionary dictates if the waypointer is active
            if (!waypointerPair.Value
                || !_prototype.Resolve(waypointerPair.Key, out var prototype)
                || !prototype.WorkOnGrid && playerXform.GridUid != null
                || !prototype.WorkInCombat && _combatMode.IsInCombatMode(player))
                continue;

            Dictionary<NetEntity, Vector2> serverPositions = [];
            // We have to break up the EntityCoordinates into Entities & Coordinates, so we can check for the EntityUid.
            if (TrackedServerCoordinates.TryGetValue(waypointerPair.Key, out var serverArray))
                serverPositions = serverArray.ToDictionary(k => k.Item1, k => k.Item2);

            var waypointQuery = _entity.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Check if the target fails/passes the whitelist/blacklist.
                if (!_whitelist.CheckBoth(target, blacklist: prototype.Blacklist, whitelist: prototype.Whitelist)
                    // Check if the target has a hidden IFF.
                    || _shuttle.HasIFFFlag(target, IFFFlags.Hide)
                    // The station grid cannot be tracked directly due to being in nullspace
                    || CheckForStation(ref target, prototype)
                    || !_entity.TryGetComponent<TransformComponent>(target, out var targetXform)
                    // Check if the target is even on the same map.
                    || targetXform.MapID != args.MapId)
                    continue;

                // Avoid drawing it twice later on, as we are in PVS range and have more accurate data.
                serverPositions.Remove(_entity.GetNetEntity(target));

                var targetPositionAndRotation = _transform.GetWorldPositionRotation(targetXform);
                var targetPosition = targetPositionAndRotation.WorldPosition;

                float distance;
                if (_entity.TryGetComponent<MapGridComponent>(target, out var map))
                {
                    // Grids take a little more work - This calculates the distance to the closest part of the grid.
                    _physics.TryGetDistance(player, target, out distance, playerXform, targetXform);
                    // And then we also want to point towards the center of the grid - Not where the entity actually is.
                    targetPosition += targetPositionAndRotation.WorldRotation.RotateVec(map.LocalAABB.Center);
                }
                else
                    // Else we simply get the distance through this.
                    distance = (playerPosition - targetPosition).Length();

                if (prototype.HideBeyondMaxRange && distance > prototype.MaxRange)
                    continue;

                DrawWaypointerArrow(prototype, distance, playerPosition, targetPosition, handle);
            }
            // This goes over every entity location sent by the server that is outside PVS range.
            // This data is very likely outdated, and that is why the client is authoritative above.
            // We don't need to check for any whitelists or map IDs as the server does that.
            // We don't need to check for grid stuff either, as grids are PVS overriden and won't be drawn here.
            foreach (var serverPosition in serverPositions)
            {
                // No need to check for grids to calculate the closest distance because of the above.
                var distance = (playerPosition - serverPosition.Value).Length();

                if (prototype.HideBeyondMaxRange && distance > prototype.MaxRange)
                    continue;

                DrawWaypointerArrow(prototype, distance, playerPosition, serverPosition.Value, handle);
            }
            // Clear the memory of drawn arrows for the next tick.
            handle.SetTransform(Matrix3x2.Identity);
        }
    }

    private void DrawWaypointerArrow(WaypointerPrototype prototype, float distance, Vector2 playerPosition, Vector2 targetPosition, DrawingHandleWorld handle)
    {
        // The WreckWaypointer has 5 stages and a range of 50. With calculations, it'll check if it's either in:
        // 0-9, 10-19, 20-29, 30-39, 40-50 range and use the respective waypointer sprite for it.
        var increments = prototype.MaxRange / prototype.WaypointerStates;
        var waypointerState = Math.Min(Math.Truncate(distance / increments) + 1, prototype.WaypointerStates);
        var stateName = "marker" + waypointerState;

        var rsi = new SpriteSpecifier.Rsi(prototype.RsiPath, stateName);
        var texture = _sprite.Frame0(rsi);

        var offset = new Vector2(texture.Height * 0.5f, texture.Width * 0.5f) / EyeManager.PixelsPerMeter;
        var direction = playerPosition - targetPosition;
        var angle = direction.ToWorldAngle();

        handle.DrawTexture(texture, playerPosition - offset, angle, prototype.Color);
    }

    /// <summary>
    /// This checks if the target is the station grid and if it should be tracking that.
    /// </summary>
    /// <param name="target">The target being tracked</param>
    /// <param name="prototype">The waypointer prototype</param>
    /// <returns>
    /// Returns true if the target is the station grid, otherwise false.
    /// The parameter target will be changed to the station grid Uid if the prototype is tracking the station grid.
    /// </returns>
    /// <remarks>
    /// The station grid is a weird exception - Tracking it directly with StationDataComponent does not work.
    /// It'll result in tracking an Entity in nullspace. The grid itself does NOT have StationDataComponent.
    /// That also carries the issue of blacklists not working against the station grid, because it doesn't have the components.
    /// So, we need to check if the station grid is being tracked, or if we wrongly tracked the station grid when we were just tracking ordinary grids.
    /// </remarks>
    private bool CheckForStation(ref EntityUid target, WaypointerPrototype prototype)
    {
        // If we are tracking the station via StationDataComponent, we will NEVER get the mainStationGrid.
        // So if we somehow DID get the station grid, it's because we are tracking something else and it bypassed the blacklist.
        if (target == _mainStationGrid)
            return true;

        // If we are supposed to track the station grid, but are tracking the station entity in nullspace, replace it.
        if (prototype.TrackedComponents.TryGetComponent<StationDataComponent>(_entity.ComponentFactory, out _) && _mainStationGrid.HasValue)
            target = _mainStationGrid.Value;

        return false;
    }

    /// <summary>
    /// Get the station grid that's on the playable map.
    /// </summary>
    /// <returns>The Uid for the station grid.</returns>
    private EntityUid? GetStationGrid()
    {
        var stationQuery = _entity.AllEntityQueryEnumerator<StationDataComponent>();

        if (!stationQuery.MoveNext(out var station, out var comp))
            return null;

        return _station.GetLargestGrid((station, comp));
    }
}
