using Content.Shared.Overlays;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Enums;

namespace Content.Server.Overlay;

public abstract class DebugOverlaySystem<TPayload> : SharedDebugOverlaySystem<TPayload>
    where TPayload : DebugOverlayPayload, new()
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly HashSet<ICommonSession> _playerObservers = new();

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

        var message = new TPayload() { OverlayEnabled = false };
        RaiseNetworkEvent(message);

        return true;
    }

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
