using Content.Shared.Overlays;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Enums;

namespace Content.Server.Overlay;

public abstract class DebugOverlaySystem<TPayload> : SharedDebugOverlaySystem<TPayload>
    where TPayload : DebugOverlayPayload, new()
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    ///     Players allowed to see the atmos debug overlay.
    ///     To modify it see <see cref="AddObserver"/> and
    ///     <see cref="RemoveObserver"/>.
    /// </summary>
    protected readonly HashSet<ICommonSession> _playerObservers = new();

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

        TPayload message = (TPayload)_sandboxHelper.CreateInstance(typeof(TPayload));
        RaiseNetworkEvent(message);

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
}
