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

    // This is used to check if a prototype is tracking the station grid.
    private readonly string _stationCompName = "StationData";
    // Caching the Uid for the station grid.
    private EntityUid? _mainStationGrid;
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
            // Check if the Waypointer hashset is null
            || waypointer.WaypointerProtoIds == null
            || !_entity.TryGetComponent<TransformComponent>(_player.LocalEntity, out var playerXform)
            || playerXform.MapID != args.MapId)
            return;

        var player = _player.LocalEntity.Value;

        foreach (var waypointerPair in waypointer.WaypointerProtoIds)
        {
            // The boolean in the dictionary describes if the waypointer is active
            if (!waypointerPair.Value)
                continue;

            if (!_prototype.Resolve(waypointerPair.Key, out var prototype)
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
                    || _shuttle.HasIFFFlag(target, IFFFlags.Hide)
                    // The station grid cannot be tracked directly due to being in nullspace
                    || CheckForStation(ref target, prototype)
                    // Check if it has the Transform Component
                    || !_entity.TryGetComponent<TransformComponent>(target, out var targetXform)
                    // Check if the target is even on the same map.
                    || targetXform.MapID != args.MapId)
                    continue;

                var positionA = _transform.GetWorldPosition(playerXform);
                var positionAndRotationB = _transform.GetWorldPositionRotation(targetXform);
                var positionB = positionAndRotationB.WorldPosition;

                float distance;
                if (_entity.TryGetComponent<MapGridComponent>(target, out var map))
                {
                    // Grids take a little more work - This calculates the distance to the closest part of the grid.
                    _physics.TryGetDistance(player, target, out distance, playerXform, targetXform);
                    // And then we also want to point towards the center of the grid - Not where the entity actually is.
                    positionB += positionAndRotationB.WorldRotation.RotateVec(map.LocalAABB.Center);
                }
                else
                    // Else we simply get the distance through this.
                    distance = (positionA - positionB).Length();

                if (distance > prototype.MaxRange)
                    continue;

                // The NTStationWaypointer has 5 stages and a range of 200. With calculations, it'll check if it's either in:
                // 0-39, 40-89, 80-119, 120-159, 160-200 range and use the respective waypointer sprite for it.
                var increments = prototype.MaxRange / prototype.WaypointerStates;
                var waypointerState = Math.Truncate(distance / increments) + 1;
                var stateName = "marker" + waypointerState;

                var rsi = new SpriteSpecifier.Rsi(prototype.RsiPath, stateName);
                var texture = _sprite.Frame0(rsi);

                // This is to draw the Waypointer sprites directly ontop of the entity sprite.
                var offset = new Vector2(texture.Height * 0.5f, texture.Width * 0.5f) / EyeManager.PixelsPerMeter;

                // This calculates the angle to rotate the waypointer sprite towards the tracked entity.
                var direction = positionA - positionB;
                var angle = direction.ToWorldAngle();

                handle.DrawTexture(texture, positionA - offset, angle, prototype.Color);
            }
            handle.SetTransform(Matrix3x2.Identity);
        }
    }

    /// <summary>
    /// This checks if the target is the station grid and if it should be tracking that.
    /// The station grid is a weird exception - Tracking it directly with StationDataComponent does not work.
    /// It'll result in tracking an Entity in nullspace. The grid itself does NOT have StationDataComponent.
    /// That also carries the issue of blacklists not working against the station grid, because it doesn't have the components.
    /// So, we need to check if the station grid is being tracked, or if we wrongly tracked the station grid when we were just tracking ordinary grids.
    /// </summary>
    /// <param name="target">The target being tracked</param>
    /// <param name="prototype">The waypointer prototype</param>
    /// <returns>
    /// Returns true if the target is the station grid, otherwise false.
    /// The parameter target will be changed to the station grid Uid if the prototype is tracking the station grid.
    /// </returns>
    private bool CheckForStation(ref EntityUid target, WaypointerPrototype prototype)
    {
        // If we are tracking the station via StationDataComponent, we will NEVER get the mainStationGrid.
        // So if we somehow DID get the station grid, it's because we are tracking something else and it bypassed the blacklist.
        if (target == _mainStationGrid)
            return true;

        // If we are supposed to track the station grid, but are tracking the station entity in nullspace, replace it.
        if (prototype.TrackedComponents.ContainsKey(_stationCompName) && _mainStationGrid.HasValue)
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
