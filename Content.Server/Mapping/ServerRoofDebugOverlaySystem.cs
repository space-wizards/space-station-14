using System.Numerics;
using Content.Server.Light.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Content.Shared.Mapping;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.Mapping;

// woe, copy pasted code from the atmos debug overlay upon ye
public sealed partial class ServerRoofDebugOverlaySystem : RoofDebugOverlaySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private RoofSystem _roof = default!;
    /// <summary>
    ///     Players allowed to see the roof debug overlay.
    ///     To modify it see <see cref="AddObserver"/> and
    ///     <see cref="RemoveObserver"/>.
    /// </summary>
    private readonly HashSet<ICommonSession> _playerObservers = new();
    /// <summary>
    ///     Overlay update ticks per second.
    /// </summary>
    private float _updateCooldown;
    private List<Entity<MapGridComponent>> _grids = new();
    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public bool AddObserver(ICommonSession observer)
    {
        return _playerObservers.Add(observer);
    }

    public bool HasObserver(ICommonSession observer)
    {
        return _playerObservers.Contains(observer);
    }

    public bool RemoveObserver(ICommonSession observer)
    {
        if (!_playerObservers.Remove(observer))
        {
            return false;
        }
        var message = new RoofDebugOverlayDisableMessage();
        RaiseNetworkEvent(message, observer.Channel);
        return true;
    }

    /// <summary>
    ///     Adds the given observer if it doesn't exist, removes it otherwise.
    /// </summary>
    /// <param name="observer">The observer to toggle.</param>
    /// <returns>true if added, false if removed.</returns>
    public bool ToggleObserver(ICommonSession observer)
    {
        if (HasObserver(observer))
        {
            RemoveObserver(observer);
            return false;
        }
        AddObserver(observer);
        return true;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
        {
            RemoveObserver(e.Session);
        }
    }

    public override void Update(float frameTime)
    {
        AccumulatedFrameTime += frameTime;
        _updateCooldown = 1 / _configManager.GetCVar(CCVars.NetAtmosDebugOverlayTickRate);
        if (AccumulatedFrameTime < _updateCooldown)
        {
            return;
        }
        // This is the timer from GasTileOverlaySystem
        AccumulatedFrameTime -= _updateCooldown;
        // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
        // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
        // Afterwards we reset all the chunk data for the next time we tick.
        foreach (var session in _playerObservers)
        {
            if (session.AttachedEntity is not {Valid: true} entity)
                continue;
            var transform = Transform(entity);
            var pos = _transform.GetWorldPosition(transform);
            var worldBounds = Box2.CenteredAround(pos,
                new Vector2(LocalViewRange, LocalViewRange));
            _grids.Clear();
            _mapSystem.FindGridsIntersecting(transform.MapID, worldBounds, ref _grids);
            foreach (var grid in _grids)
            {
                var uid = grid.Owner;
                if (!Exists(uid))
                    continue;
                TryComp(uid, out RoofComponent? roof);
                var entityTile = _mapSystem.GetTileRef(grid, grid, transform.Coordinates).GridIndices;
                var baseTile = new Vector2i(entityTile.X - LocalViewRange / 2, entityTile.Y - LocalViewRange / 2);
                var debugOverlayContent = new RoofDebugOverlayData?[LocalViewRange * LocalViewRange];
                var index = 0;
                var isInnate = HasComp<ImplicitRoofComponent>(uid);
                for (var y = 0; y < LocalViewRange; y++)
                {
                    for (var x = 0; x < LocalViewRange; x++)
                    {
                        var vector = new Vector2i(baseTile.X + x, baseTile.Y + y);
                        if (roof != null)
                        {
                            var roofed = _roof.IsRooved((grid.Owner, grid.Comp, roof), vector, false);
                            var roofedEnt = _roof.IsRoovedEntity((grid.Owner, grid.Comp, roof), vector);
                            debugOverlayContent[index++] = new RoofDebugOverlayData(vector, roofed, roofedEnt, isInnate);
                        }
                        else
                            debugOverlayContent[index++] = new RoofDebugOverlayData(vector, false, false, isInnate);
                    }
                }
                var msg = new RoofDebugOverlayMessage(GetNetEntity(grid), baseTile, debugOverlayContent);
                RaiseNetworkEvent(msg, session.Channel);
            }
        }
    }
}

